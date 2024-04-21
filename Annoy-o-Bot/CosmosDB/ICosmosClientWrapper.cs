
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Annoy_o_Bot.CosmosDB;

public interface ICosmosClientWrapper
{
    Task<IList<ReminderDocument>> LoadAllReminders(Container cosmosClient);
    Task<ReminderDocument?> LoadReminder(Container cosmosClient, string fileName, long installationId, long repositoryId);
    Task Delete(Container cosmosClient, string fileName, long installationId, long repositoryId);
    Task AddOrUpdateReminder(Container cosmosClient, ReminderDocument reminderDocument);
}
