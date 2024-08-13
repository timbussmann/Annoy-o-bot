using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Annoy_o_Bot.GitHub.Callbacks;

public class GitHubCallbackRequest
{
    public static bool IsGitCommitCallback(HttpRequest callbackRequest, ILogger log)
    {
        if (!callbackRequest.Headers.TryGetValue("X-GitHub-Event", out var callbackEvent) || callbackEvent != "push")
        {
            // Check for known callback types that we don't care
            if (callbackEvent != "check_suite") // ignore check_suite events
            {
                // record unknown callback types to further analyze them
                log.LogWarning($"Non-push callback. 'X-GitHub-Event': '{callbackEvent}'");
            }

            return false;
        }

        return true;
    }
    
    public static async Task<CallbackModel> Validate(HttpRequest callbackRequest, string gitHubSecret)
    {
        if (!callbackRequest.Headers.TryGetValue("X-Hub-Signature-256", out var sha256SignatureHeaderValue))
        {
            throw new Exception("Incoming callback request does not contain a 'X-Hub-Signature-256' header");
        }

        var requestBody = await new StreamReader(callbackRequest.Body).ReadToEndAsync();
            
        await ValidateSignature(requestBody, gitHubSecret, sha256SignatureHeaderValue.ToString().Replace("sha256=", ""));

        return CallbackModel.FromJson(requestBody);
    }
    
    public static async Task ValidateSignature(string signedText, string secret, string sha256Signature)
    {
        var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(signedText));

        var hash = await hmacsha256.ComputeHashAsync(memoryStream);
        var hashString = Convert.ToHexString(hash);

        if (!string.Equals(sha256Signature, hashString, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Computed request payload signature ('{hashString}') does not match provided signature ('{sha256Signature}'). Signed text: '{signedText}'");
        }
    }
}