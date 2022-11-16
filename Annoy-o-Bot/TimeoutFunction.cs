using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot
{
    public class TimeoutFunction
    {
        private ICosmosClientWrapper cosmosWrapper;
        private Func<long, long, IGitHubAppInstallation> installationClientFactory;

        public TimeoutFunction(Func<long, long, IGitHubAppInstallation> installationClientFactory)
        {
            this.cosmosWrapper = new CosmosClientWrapper();
            this.installationClientFactory = installationClientFactory;
        }

        [FunctionName("TimeoutFunction")]
        public async Task Run(
            //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [TimerTrigger("0 */10 * * * *", RunOnStartup = false)]TimerInfo timer, // once every 10 minutes
            [CosmosDB(CosmosClientWrapper.dbName,
                CosmosClientWrapper.collectionId,
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = CosmosClientWrapper.ReminderQuery)]
            IEnumerable<ReminderDocument> dueReminders,
            [CosmosDB(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId, ConnectionStringSetting = "CosmosDBConnection")] IDocumentClient documentClient,
            ILogger log)
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

                        var installationClient = installationClientFactory(reminder.InstallationId, reminder.RepositoryId);
                        var issue = await installationClient.CreateIssue(newIssue);

                        log.LogInformation($"Created reminder issue #{issue.Number} based on reminder {reminder.Id}");

                        reminder.LastReminder = now;
                        await cosmosWrapper.AddOrUpdateReminder(documentClient, reminder);
                    }
                    else
                    {
                        // Next Reminder might have been reset due to an update, so we will just recalculate it.
                        reminder.CalculateNextReminder(now);
                        log.LogWarning($"Found LastReminder ({reminder.LastReminder:g}) > NextReminder ({reminder.NextReminder:g}) in reminder {reminder.Id}");
                        await cosmosWrapper.AddOrUpdateReminder(documentClient, reminder);
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