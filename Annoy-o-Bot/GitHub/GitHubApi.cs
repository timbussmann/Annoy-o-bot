using System;
using System.Threading.Tasks;
using GitHubJwt;
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