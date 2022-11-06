using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Annoy_o_Bot.AcceptanceTests;

public class CallbackHandlerTest
{
    protected const string SignatureKey = "mysecretkey";

    protected ConfigurationBuilder configurationBuilder;

    public CallbackHandlerTest()
    {
        configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "WebhookSecret", SignatureKey }
        });
    }

    protected static HttpRequest CreateGitHubCallbackRequest(CallbackModel callback)
    {
        HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext());
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(callback, Formatting.None));
        request.Body = new MemoryStream(messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256",
            "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));
        return request;
    }

    protected static CallbackModel CreateGitHubCallbackModel(string defaultBranch = "main", string? currentBranch = null, params CallbackModel.CommitModel[] commits)
    {
        return new CallbackModel
        {
            Installation = new CallbackModel.InstallationModel() { Id = Random.Shared.NextInt64() },
            Repository = new CallbackModel.RepositoryModel() { Id = Random.Shared.NextInt64(), DefaultBranch = defaultBranch },
            Ref = $"refs/heads/{currentBranch ?? defaultBranch}",
            HeadCommit = commits[0],
            Commits = commits
        };
    }
}