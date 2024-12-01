
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Annoy_o_Bot.CosmosDB;

public interface ICosmosClientWrapper
{
    Task<IList<ReminderDocument>> LoadAllReminders();
    Task<ReminderDocument?> LoadReminder(string fileName, long installationId, long repositoryId);
    Task Delete(string fileName, long installationId, long repositoryId);
    Task AddOrUpdateReminder(ReminderDocument reminderDocument);
}
