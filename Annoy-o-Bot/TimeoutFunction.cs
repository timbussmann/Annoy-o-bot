using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Octokit;
using Microsoft.Azure.Functions.Worker;

namespace Annoy_o_Bot
{
    public class TimeoutFunction
    {
        readonly IGitHubApi gitHubApi;
        readonly ICosmosClientWrapper cosmosWrapper;
        readonly ILogger<TimeoutFunction> log;

        public TimeoutFunction(IGitHubApi gitHubApi, ILogger<TimeoutFunction> log)
        {
            this.gitHubApi = gitHubApi;
            this.log = log;
            this.cosmosWrapper = new CosmosClientWrapper();
        }

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
            foreach (var reminder in dueReminders)
            {
                try
                {
                    // round down to the current hour
                    var now = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);

                    // either never created a reminder before (and next reminder has elapsed)
                    // or the last created reminder was before next reminder (and reminder has elapsed)
                    if (reminder.LastReminder < reminder.NextReminder)
                    {
                        reminder.CalculateNextReminder(now);

                        var newIssue = new NewIssue(reminder.Reminder.Title)
                        {
                            Body = reminder.Reminder.Message,
                        };
                        foreach (var assignee in reminder.Reminder.Assignee?.Split(';',
                                     StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>())
                        {
                            newIssue.Assignees.Add(assignee);
                        }

                        foreach (var label in reminder.Reminder.Labels)
                        {
                            newIssue.Labels.Add(label);
                        }

                        log.LogDebug($"Scheduling next due date for reminder {reminder.Id} for {reminder.NextReminder}");

                        var repository = await gitHubApi.GetRepository(reminder.InstallationId, reminder.RepositoryId);
                        var issue = await repository.CreateIssue(newIssue);

                        log.LogInformation($"Created reminder issue #{issue.Number} based on reminder {reminder.Id}");

                        reminder.LastReminder = now;
                        await cosmosWrapper.AddOrUpdateReminder(cosmosContainer, reminder);
                    }
                    else
                    {
                        // Next Reminder might have been reset due to an update, so we will just recalculate it.
                        reminder.CalculateNextReminder(now);
                        log.LogWarning($"Found LastReminder ({reminder.LastReminder:g}) > NextReminder ({reminder.NextReminder:g}) in reminder {reminder.Id}");
                        await cosmosWrapper.AddOrUpdateReminder(cosmosContainer, reminder);
                    }
                }
                catch (ApiValidationException validationException)
                {
                    log.LogCritical(validationException,$"ApiValidation on reminder '{reminder.Id}' exception: {validationException.Message}:{validationException.HttpResponse.Body}");
                }
                catch (Exception e)
                {
                    log.LogCritical(e, $"Failed to create reminder for {reminder.Id}");
                }
            }
        }
    }
}