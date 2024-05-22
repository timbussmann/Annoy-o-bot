using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Annoy_o_Bot.GitHub.Callbacks;
using GitHubJwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public class GitHubApi : IGitHubApi
{
    ILoggerFactory loggerFactory;
    ILogger<GitHubApi> logger;

    public GitHubApi(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<GitHubApi>();
    }

    public async Task<IGitHubInstallation> GetInstallation(long installationId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubInstallation(installationClient, installationId, loggerFactory);
    }

    public async Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubRepository(installationClient, repositoryId, loggerFactory.CreateLogger<GitHubRepository>());
    }

    public async Task<CallbackModel> ValidateCallback(HttpRequest callbackRequest)
    {
        if (!callbackRequest.Headers.TryGetValue("X-Hub-Signature-256", out var sha256SignatureHeaderValue))
        {
            throw new Exception("Incoming callback request does not contain a 'X-Hub-Signature-256' header");
        }

        var requestBody = await new StreamReader(callbackRequest.Body).ReadToEndAsync();

        var secret = Environment.GetEnvironmentVariable("WebhookSecret") ??
                     throw new Exception("Missing 'WebhookSecret' setting to validate GitHub callbacks.");
            
        await ValidateSignature(requestBody, secret, sha256SignatureHeaderValue.ToString().Replace("sha256=", ""));

        return RequestParser.ParseJson(requestBody);
    }

    public async Task ValidateSignature(string signedText, string secret, string sha256Signature)
    {
        var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(signedText));

        var hash = await hmacsha256.ComputeHashAsync(memoryStream);
        var hashString = Convert.ToHexString(hash);

        if (!string.Equals(sha256Signature, hashString, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Invalid request body that doesn't match provided SHA256 signature ({shaSignature}): {body}",
                sha256Signature, signedText);
            throw new Exception(
                $"Computed request payload signature ('{hashString}') does not match provided signature ('{sha256Signature}')");
        }
    }

    static async Task<GitHubClient> GetInstallationClient(long installationId)
    {
        // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
        var appIntegrationId = Convert.ToInt32(Environment.GetEnvironmentVariable("GitHubAppId"));
        var environmentVariablePrivateKeySource = new EnvironmentVariablePrivateKeySource("PrivateKey");
        var generator = new GitHubJwtFactory(
            environmentVariablePrivateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = appIntegrationId,
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            }
        );
        var jwtToken = generator.CreateEncodedJwtToken();

        var productHeaderValue = Environment.GetEnvironmentVariable("GitHubProductHeader");
        // Use the JWT as a Bearer token
        var appClient = new GitHubClient(new ProductHeaderValue(productHeaderValue))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };
        // Get the current authenticated GitHubApp
        var app = await appClient.GitHubApps.GetCurrent();
        // Create an Installation token for Insallation Id
        var response = await appClient.GitHubApps.CreateInstallationToken(installationId);

        // Create a new GitHubClient using the installation token as authentication
        var installationClient = new GitHubClient(new ProductHeaderValue(productHeaderValue))
        {
            Credentials = new Credentials(response.Token)
        };
        return installationClient;
    }
}