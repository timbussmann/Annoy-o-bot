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
            [CosmosDB("annoydb", 
                "reminders", 
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = @"SELECT * FROM c WHERE GetCurrentDateTime() >= c.NextReminder AND c.LastReminder < c.NextReminder")] 
            IEnumerable<ReminderDocument> dueReminders,
            [CosmosDB("annoydb",
                "reminders",
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
                    reminder.LastReminder = now;

                    CalculateNextReminder(reminder, now);

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

                    log.LogDebug($"Scheduling next due date for reminder {reminder.Id} for {reminder.NextReminder}");

                    var issue = await installationClient.Issue.Create(reminder.RepositoryId, newIssue);

                    log.LogInformation($"Created reminder issue based on reminder {reminder.Id}");

                    await documents.AddAsync(reminder);
                }
                catch (ApiValidationException validationException)
                {
                    log.LogCritical(validationException,$"ApiValidation on reminder '{reminder.Id}' exception: {validationException.Message}:{validationException.HttpResponse}");
                }
                catch (Exception e)
                {
                    log.LogCritical(e, $"Failed to create reminder for {reminder.Id}");
                }
            }
        }

        public static void CalculateNextReminder(ReminderDocument reminder, DateTime now)
        {
            var intervalSteps = Math.Max(reminder.Reminder.IntervalStep ?? 1, 1);
            for (int i = 0; i < intervalSteps; i++)
            {
                switch (reminder.Reminder.Interval)
                {
                    case Interval.Once:
                        break;
                    case Interval.Daily:
                        reminder.NextReminder =
                            GetNextReminderDate(x => x.AddDays(intervalSteps));
                        break;
                    case Interval.Weekly:
                        reminder.NextReminder =
                            GetNextReminderDate(x => x.AddDays(7 * intervalSteps));
                        break;
                    case Interval.Monthly:
                        reminder.NextReminder =
                            GetNextReminderDate(x => x.AddMonths(intervalSteps));
                        break;
                    case Interval.Yearly:
                        reminder.NextReminder =
                            GetNextReminderDate(x => x.AddYears(intervalSteps));
                        break;
                    default:
                        throw new ArgumentException($"Invalid reminder interval {reminder.Reminder.Interval}");
                }
            }

            DateTime GetNextReminderDate(Func<DateTime, DateTime> incrementFunc)
            {
                var next = reminder.NextReminder;
                while (next <= now)
                {
                    next = incrementFunc(next);
                }
                return next;
            }
        }
    }
}

//TODO unbounded query
