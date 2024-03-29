﻿using System.Collections.ObjectModel;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
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

    protected DocumentClient documentClient;

    public AcceptanceTest(CosmosFixture cosmosFixture)
    {
        configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "WebhookSecret", SignatureKey }
        });

        documentClient = cosmosFixture.CreateDocumentClient();
        SetupCollection().GetAwaiter().GetResult();
    }

    async Task SetupCollection()
    {
        try
        {
            await documentClient.DeleteDocumentCollectionAsync(
                UriFactory.CreateDocumentCollectionUri(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId),
                new RequestOptions() { });
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound)
            {
                throw;
            }
        }

        await documentClient.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(CosmosClientWrapper.dbName),
            new DocumentCollection()
            {
                Id = CosmosClientWrapper.collectionId,
                PartitionKey = new PartitionKeyDefinition() { Paths = new Collection<string>() { "/id" } }
            });
    }

    protected async Task CreateDueReminders(IGitHubApi gitHubApi)
    {
        var timeoutHandler = new TimeoutFunction(gitHubApi);

        var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId);
        var result = documentClient.CreateDocumentQuery<ReminderDocument>(
            documentCollectionUri,
            CosmosClientWrapper.ReminderQuery,
            new FeedOptions { EnableCrossPartitionQuery = true });

        await timeoutHandler.Run(null!, result, documentClient, NullLogger.Instance);
    }

    protected static HttpRequest CreateCallbackHttpRequest(CallbackModel callback)
    {
        HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext());
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(callback, Formatting.None));
        request.Body = new MemoryStream(messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256",
            "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));
        return request;
    }
}