using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace Annoy_o_Bot.CosmosDB;

public interface ICosmosClientWrapper
{
    Task<IList<ReminderDocument>> LoadAllReminders(IDocumentClient cosmosClient);
    Task<ReminderDocument?> LoadReminder(IDocumentClient cosmosClient, string fileName, long installationId, long repositoryId);
    Task Delete(IDocumentClient cosmosClient, string fileName, long installationId, long repositoryId);
    Task AddOrUpdateReminder(IDocumentClient documentClient, ReminderDocument reminderDocument);
}
