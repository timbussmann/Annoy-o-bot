using System.Net;
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
public class AcceptanceTest
{
    protected const string SignatureKey = "mysecretkey";

    protected ConfigurationBuilder configurationBuilder;

    protected Container container;

    public AcceptanceTest(CosmosFixture cosmosFixture)
    {
        configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "WebhookSecret", SignatureKey }
        });

        container = cosmosFixture.CreateDocumentClient();
        SetupCollection().GetAwaiter().GetResult();
    }

    async Task SetupCollection()
    {
        var database = container.Database;
        try
        {
            await database.GetContainer(CosmosClientWrapper.collectionId).DeleteContainerAsync();
        }
        catch (CosmosException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound)
            {
                throw;
            }
        }

        await database.CreateContainerAsync(CosmosClientWrapper.collectionId, "/id");
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

    protected static HttpRequest CreateCallbackHttpRequest(CallbackModel callback)
    {
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(callback, Formatting.None));
        request.Body = new MemoryStream(messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256",
            "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));
        return request;
    }
}