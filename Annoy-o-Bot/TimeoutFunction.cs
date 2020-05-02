using System;
using System.Collections.Generic;
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
                // round down to the current hour
                var now = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
                reminder.LastReminder = now;

                //reminder.NextReminder
                var intervalSteps = reminder.Reminder.IntervalStep ?? 1;
                for (int i = 0; i < intervalSteps; i++)
                {
                    switch (reminder.Reminder.Interval)
                    {
                        case Interval.Once:
                            break;
                        case Interval.Daily:
                            reminder.NextReminder = reminder.NextReminder.AddDays(1);
                            break;
                        case Interval.Weekly:
                            reminder.NextReminder = reminder.NextReminder.AddDays(7);
                            break;
                        case Interval.Monthly:
                            reminder.NextReminder = reminder.NextReminder.AddMonths(1);
                            break;
                        case Interval.Yearly:
                            reminder.NextReminder = reminder.NextReminder.AddYears(1);
                            break;
                        default: 
                            throw new ArgumentException($"Invalid reminder interval {reminder.Reminder.Interval}");
                    }
                }

                var installationClient = await GitHubHelper.GetInstallationClient(reminder.InstallationId);
                var newIssue = new NewIssue(reminder.Reminder.Title)
                {
                    Body = reminder.Reminder.Message,
                };
                foreach (var assignee in reminder.Reminder.Assignee.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    newIssue.Assignees.Add(assignee);
                }
                log.LogDebug($"Scheduling next due date for reminder {reminder.Id} for {reminder.NextReminder}");

                var issue = await installationClient.Issue.Create(reminder.RepositoryId, newIssue);

                log.LogInformation($"Created reminder issue based on reminder {reminder.Id}");

                await documents.AddAsync(reminder);
            }
        }
    }
}

//TODO unbounded query
