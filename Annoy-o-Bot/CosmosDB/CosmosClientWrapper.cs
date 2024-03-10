using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Annoy_o_Bot.CosmosDB;

public class CosmosClientWrapper : ICosmosClientWrapper
{
    public const string dbName = "annoydb";
    public const string collectionId = "reminders";

    public const string ReminderQuery = "SELECT TOP 50 * FROM c WHERE GetCurrentDateTime() >= c.NextReminder ORDER BY c.NextReminder ASC";

    public Task<IList<ReminderDocument>> LoadAllReminders(IDocumentClient cosmosClient)
    {
        var query = cosmosClient
            .CreateDocumentQuery<ReminderDocument>(UriFactory.CreateDocumentCollectionUri(dbName, collectionId))
            .ToList();

        return Task.FromResult<IList<ReminderDocument>>(query);
    }

    public async Task<ReminderDocument?> LoadReminder(IDocumentClient cosmosClient, string fileName, long installationId,
        long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);
        var documentUri = UriFactory.CreateDocumentUri(dbName, collectionId, documentId);

        // TODO: Cosmos v3 SDK should allow to check without a try-catch block
        try
        {
            var existingReminder = await cosmosClient.ReadDocumentAsync<ReminderDocument>(
                documentUri,
                new RequestOptions
                {
                    PartitionKey = new PartitionKey(documentId)
                });
            return existingReminder.Document;
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw;
        }
    }

    public async Task Delete(IDocumentClient cosmosClient, string fileName, long installationId, long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);
        var documentUri = UriFactory.CreateDocumentUri(dbName, collectionId, documentId);
        await cosmosClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(documentId) });
    }

    public async Task AddOrUpdateReminder(IDocumentClient documentClient, ReminderDocument reminderDocument)
    {
        reminderDocument.Id ??= BuildDocumentId(reminderDocument.Path, reminderDocument.InstallationId, reminderDocument.RepositoryId);
        var collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionId);
        try
        {
            await documentClient.UpsertDocumentAsync(collectionUri, reminderDocument, new RequestOptions() {});
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    static string BuildDocumentId(string fileName, long installationId, long repositoryId)
    {
        return $"{installationId}-{repositoryId}-{fileName.Split('/').Last()}";
    }
}