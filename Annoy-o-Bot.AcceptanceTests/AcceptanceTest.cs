using System.Security.Cryptography;
using System.Text;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Annoy_o_Bot.GitHub.Callbacks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

[Collection("CosmosDB")]
public class AcceptanceTest(CosmosFixture cosmosFixture) : IAsyncLifetime
{
    protected const string SignatureKey = "mysecretkey";

    protected ConfigurationBuilder configurationBuilder;

    protected Container container;

    public async Task InitializeAsync()
    {
        configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "WebhookSecret", SignatureKey }
        });

        container = cosmosFixture.CreateDocumentClient();
        await SetupCollection();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await container.DeleteContainerAsync();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    async Task SetupCollection()
    {
        var database = container.Database;
        await database.Client.CreateDatabaseIfNotExistsAsync(CosmosClientWrapper.dbName);
        await database.CreateContainerIfNotExistsAsync(CosmosClientWrapper.collectionId, "/id");
    }

    protected async Task CreateDueReminders(IGitHubApi gitHubApi)
    {
        var queryIterator = container
            .GetItemQueryIterator<ReminderDocument>(CosmosClientWrapper.ReminderQuery);
        List<ReminderDocument> reminders = new();
        while (queryIterator.HasMoreResults)
        {
            var readResult = await queryIterator.ReadNextAsync();
            reminders.AddRange(readResult.Resource);
        }

        var timeoutHandler = new TimeoutFunction(gitHubApi, NullLogger<TimeoutFunction>.Instance);

        await timeoutHandler.Run(null!, reminders, container);
    }

    protected static HttpRequest CreateCallbackHttpRequest(GitPushCallbackModel gitPushCallback)
    {
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gitPushCallback, Formatting.None));
        request.Body = new MemoryStream(messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256",
            "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));
        return request;
    }
}