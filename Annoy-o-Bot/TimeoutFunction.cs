using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot
{
    public static class TimeoutFunction
    {
        [FunctionName("TimeoutFunction")]
        public static async Task Run(
            //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [TimerTrigger("0 */10 * * * *", RunOnStartup = false)]TimerInfo timer, // once every 10 minutes
            [CosmosDB(CallbackHandler.dbName,
                CallbackHandler.collectionId,
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "SELECT TOP 50 * FROM c WHERE GetCurrentDateTime() >= c.NextReminder ORDER BY c.NextReminder ASC")]
            IEnumerable<ReminderDocument> dueReminders,
            [CosmosDB(CallbackHandler.dbName,
                CallbackHandler.collectionId,
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<ReminderDocument> documents,
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

                        var installationClient = await GitHubHelper.GetInstallationClient(reminder.InstallationId);
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

                        var issue = await installationClient.Issue.Create(reminder.RepositoryId, newIssue);

                        log.LogInformation($"Created reminder issue #{issue.Number} based on reminder {reminder.Id}");

                        reminder.LastReminder = now;
                        await documents.AddAsync(reminder);
                    }
                    else
                    {
                        // Next Reminder might have been reset due to an update, so we will just recalculate it.
                        reminder.CalculateNextReminder(now);
                        log.LogWarning($"Found LastReminder ({reminder.LastReminder:g}) > NextReminder ({reminder.NextReminder:g}) in reminder {reminder.Id}");
                        await documents.AddAsync(reminder);
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