using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GitHubJwt;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot
{
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Http;

    public class GitHubHelper
    {
        /// <summary>
        /// Validates whether the request is indeed coming from GitHub using the webhook secret.
        /// </summary>
        public static async Task ValidateRequest(HttpRequest request, string secret, ILogger logger)
        {
            if (!request.Headers.TryGetValue("X-Hub-Signature-256", out var sha256Signature))
            {
                throw new Exception("Incoming callback request does not contain a 'X-Hub-Signature-256' header");
            }

            var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

            // enable buffering so we can reset the request body stream position
            // otherwise this throws a System.NotSupportedException when running in Azure Functions
            request.EnableBuffering();

            var hash = await hmacsha256.ComputeHashAsync(request.Body);
            request.Body.Position = 0;
            var hashString = $"sha256={Convert.ToHexString(hash)}";

            if (!string.Equals(sha256Signature, hashString, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning($"Validation mismatch. {Environment.MachineName}, {Environment.OSVersion}, {Environment.Version}, {RuntimeInformation.RuntimeIdentifier}, {RuntimeInformation.OSArchitecture}, {RuntimeInformation.OSDescription}, {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture}");
                var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
                if (ValidateRequestSha1(request, hmacsha1))
                {
                    logger.LogWarning("Failed SHA256 validation but passed SHA1 check.");
                    return;
                }

                var exception = new Exception($"Computed request payload signature ('{hashString}') does not match provided signature ('{sha256Signature}')");
                logger.LogWarning(new StreamReader(request.Body).ReadToEnd());
                throw exception;
            }
        }

        public static bool ValidateRequestSha1(HttpRequest request, HMACSHA1 sha1)
        {
            if (!request.Headers.TryGetValue("X-Hub-Signature", out var sha1Signature))
            {
                return false;
            }

            var hash = sha1?.ComputeHash(request.Body) ?? Array.Empty<byte>();
            request.Body.Position = 0;
            var hexString = Convert.ToHexString(hash);
            var hashString = $"sha1={hexString}";

            return string.Equals(sha1Signature, hashString, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<GitHubClient> GetInstallationClient(long installationId)
        {
            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
            var generator = new GitHubJwtFactory(
                new EnvironmentVariablePrivateKeySource("PrivateKey"),
                new GitHubJwtFactoryOptions
                {
                    AppIntegrationId = Convert.ToInt32(Environment.GetEnvironmentVariable("GitHubAppId")),
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
}