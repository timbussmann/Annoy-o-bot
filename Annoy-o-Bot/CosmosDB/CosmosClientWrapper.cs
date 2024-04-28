using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Annoy_o_Bot.CosmosDB;

public class CosmosClientWrapper(Container cosmosContainer) : ICosmosClientWrapper
{
    public const string dbName = "annoydb";
    public const string collectionId = "reminders";

    public const string ReminderQuery = "SELECT TOP 50 * FROM c WHERE GetCurrentDateTime() >= c.NextReminder ORDER BY c.NextReminder ASC";

    public async Task<IList<ReminderDocument>> LoadAllReminders()
    {
        var result = new List<ReminderDocument>();
        var queryFeed = cosmosContainer.GetItemQueryIterator<ReminderDocument>();
        while (queryFeed.HasMoreResults)
        {
            var queryResponse = await queryFeed.ReadNextAsync();
            result.AddRange(queryResponse);
        }

        return result;
    }

    public async Task<ReminderDocument?> LoadReminder(string fileName, long installationId, long repositoryId)
    {
        var documentId = ReminderDocument.BuildDocumentId(fileName, installationId, repositoryId);

        try
        {
            var existingReminder = await cosmosContainer.ReadItemAsync<ReminderDocument>(documentId, new PartitionKey(documentId));
            return existingReminder.Resource;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw;
        }
    }

    public async Task Delete(string fileName, long installationId, long repositoryId)
    {
        var documentId = ReminderDocument.BuildDocumentId(fileName, installationId, repositoryId);
        await cosmosContainer.DeleteItemAsync<ReminderDocument>(documentId, new PartitionKey(documentId));
    }

    public async Task AddOrUpdateReminder(ReminderDocument reminderDocument)
    {
        try
        {
            await cosmosContainer.UpsertItemAsync(reminderDocument);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}