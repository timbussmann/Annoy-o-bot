using System;
using System.Text;
using System.Threading.Tasks;
using GitHubJwt;
using Octokit;

namespace Annoy_o_Bot
{
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Http;

    public class GitHubHelper
    {
        static string? callbackSecret = Environment.GetEnvironmentVariable("WebhookSecret");
        internal static HMACSHA256? HMAC = callbackSecret != null ? new HMACSHA256(Encoding.UTF8.GetBytes(callbackSecret)) : null;

        /// <summary>
        /// Validates whether the request is indeed coming from GitHub using the webhook secret.
        /// </summary>
        public static void ValidateRequest(HttpRequest request, HMACSHA256? hmac)
        {
            if (!request.Headers.TryGetValue("X-Hub-Signature-256", out var callbackSignature))
            {
                throw new Exception("Incoming callback request does not contain a 'X-Hub-Signature' header");
            }

            var hash = hmac?.ComputeHash(request.Body) ?? Array.Empty<byte>();
            request.Body.Position = 0;
            var hashString = $"sha256={Convert.ToHexString(hash)}";


            if (!string.Equals(callbackSignature, hashString, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Request payload body signature ('{hashString}') does not match provided signature ({callbackSignature})");
            }
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