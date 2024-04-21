﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Annoy_o_Bot.CosmosDB;

public class CosmosClientWrapper : ICosmosClientWrapper
{
    public const string dbName = "annoydb";
    public const string collectionId = "reminders";

    public const string ReminderQuery = "SELECT TOP 50 * FROM c WHERE GetCurrentDateTime() >= c.NextReminder ORDER BY c.NextReminder ASC";

    public async Task<IList<ReminderDocument>> LoadAllReminders(Container cosmosClient)
    {
        var result = new List<ReminderDocument>();
        var queryFeed = cosmosClient.GetItemQueryIterator<ReminderDocument>();
        while (queryFeed.HasMoreResults)
        {
            var queryResponse = await queryFeed.ReadNextAsync();
            result.AddRange(queryResponse);
        }

        return result;
    }

    public async Task<ReminderDocument?> LoadReminder(Container cosmosClient, string fileName, long installationId, long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);

        try
        {
            var existingReminder = await cosmosClient.ReadItemAsync<ReminderDocument>(documentId, new PartitionKey(documentId));
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

    public async Task Delete(Container cosmosClient, string fileName, long installationId, long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);
        await cosmosClient.DeleteItemAsync<ReminderDocument>(documentId, new PartitionKey(documentId));
    }

    public async Task AddOrUpdateReminder(Container cosmosClient, ReminderDocument reminderDocument)
    {
        reminderDocument.Id ??= BuildDocumentId(reminderDocument.Path, reminderDocument.InstallationId, reminderDocument.RepositoryId);
        //var collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionId);
        try
        {
            await cosmosClient.UpsertItemAsync(reminderDocument);
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