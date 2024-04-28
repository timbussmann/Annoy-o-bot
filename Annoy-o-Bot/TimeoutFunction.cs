using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Octokit;
using Microsoft.Azure.Functions.Worker;

namespace Annoy_o_Bot
{
    public class TimeoutFunction(IGitHubApi gitHubApi, ILogger<TimeoutFunction> log)
    {
        [Function("TimeoutFunction")]
        public async Task Run(
            //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [TimerTrigger("0 */10 * * * *", RunOnStartup = false)]TimerInfo timer, // once every 10 minutes
            [CosmosDBInput(
                databaseName: CosmosClientWrapper.dbName,
                containerName: CosmosClientWrapper.collectionId,
                Connection = "CosmosDBConnection",
                SqlQuery = CosmosClientWrapper.ReminderQuery)]
            IEnumerable<ReminderDocument> dueReminders,
            [CosmosDBInput(
                databaseName: CosmosClientWrapper.dbName,
                containerName: CosmosClientWrapper.collectionId,
                Connection = "CosmosDBConnection")]
            Container cosmosContainer)
        {
            var cosmosWrapper = new CosmosClientWrapper(cosmosContainer);

            foreach (var reminderDocument in dueReminders)
            {
                try
                {
                    // round down to the current hour
                    var now = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);

                    // either never created a reminder before (and next reminder has elapsed)
                    // or the last created reminder was before next reminder (and reminder has elapsed)
                    if (reminderDocument.LastReminder < reminderDocument.NextReminder)
                    {
                        reminderDocument.CalculateNextReminder(now);

                        var newIssue = reminderDocument.Reminder.ToGitHubIssue();

                        log.LogDebug($"Scheduling next due date for reminder {reminderDocument.Id} for {reminderDocument.NextReminder}");

                        var repository = await gitHubApi.GetRepository(reminderDocument.InstallationId, reminderDocument.RepositoryId);
                        var issue = await repository.CreateIssue(newIssue);

                        log.LogInformation($"Created reminder issue #{issue.Number} based on reminder {reminderDocument.Id}");

                        reminderDocument.LastReminder = now;
                        await cosmosWrapper.AddOrUpdateReminder(reminderDocument);
                    }
                    else
                    {
                        // Next Reminder might have been reset due to an update, so we will just recalculate it.
                        reminderDocument.CalculateNextReminder(now);
                        log.LogWarning($"Found LastReminder ({reminderDocument.LastReminder:g}) > NextReminder ({reminderDocument.NextReminder:g}) in reminder {reminderDocument.Id}");
                        await cosmosWrapper.AddOrUpdateReminder(reminderDocument);
                    }
                }
                catch (ApiValidationException validationException)
                {
                    log.LogCritical(validationException,$"ApiValidation on reminder '{reminderDocument.Id}' exception: {validationException.Message}:{validationException.HttpResponse.Body}");
                }
                catch (Exception e)
                {
                    log.LogCritical(e, $"Failed to create reminder for {reminderDocument.Id}");
                }
            }
        }
    }
}