using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Annoy_o_Bot.CosmosDB
{
    public class ReminderDocument
    {
        public static ReminderDocument New(long installationId, long repositoryId, string fileName, ReminderDefinition reminderDefinition)
        {
            return new ReminderDocument
            {
                Id = BuildDocumentId(fileName, installationId, repositoryId),
                InstallationId = installationId,
                RepositoryId = repositoryId,
                Path = fileName,
                Reminder = reminderDefinition,
                NextReminder = new DateTime(reminderDefinition.Date.Ticks, DateTimeKind.Utc)
            };
        }

        // assigning null using the null-forgiving operator because the value will always be set
        //[JsonProperty("id")] Newtonsoft no longer used
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        public required ReminderDefinition Reminder { get; set; }
        public required long InstallationId { get; set; }
        public required long RepositoryId { get; set; }
        public DateTime LastReminder { get; set; }
        public DateTime? NextReminder { get; set; }
        public required string Path { get; set; }

        public void CalculateNextReminder(DateTime now)
        {
            var intervalSteps = Math.Max(Reminder.IntervalStep ?? 1, 1);
            for (var i = 0; i < intervalSteps; i++)
            {
                switch (Reminder.Interval)
                {
                    case Interval.Once:
                        NextReminder = null;
                        break;
                    case Interval.Daily:
                        NextReminder = GetNextReminderDate(x => x.AddDays(intervalSteps));
                        break;
                    case Interval.Weekly:
                        NextReminder = GetNextReminderDate(x => x.AddDays(7 * intervalSteps));
                        break;
                    case Interval.Monthly:
                        NextReminder = GetNextReminderDate(x => x.AddMonths(intervalSteps));
                        break;
                    case Interval.Yearly:
                        NextReminder = GetNextReminderDate(x => x.AddYears(intervalSteps));
                        break;
                    default:
                        throw new ArgumentException($"Invalid reminder interval {Reminder.Interval}");
                }
            }

            DateTime GetNextReminderDate(Func<DateTime, DateTime> incrementFunc)
            {
                var next = NextReminder!.Value;
                while (next <= now)
                {
                    next = incrementFunc(next);
                }
                return next;
            }
        }

        public static string BuildDocumentId(string fileName, long installationId, long repositoryId)
        {
            return $"{installationId}-{repositoryId}-{fileName.Split('/').Last()}";
        }
    }
}