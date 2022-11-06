using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace Annoy_o_Bot.CosmosDB;

public interface ICosmosClientWrapper
{
    Task<ReminderDocument> LoadReminder(IDocumentClient cosmosClient, string fileName, long installationId,
        long repositoryId);
}
