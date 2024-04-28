using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Annoy_o_Bot.CosmosDB
{
    public class ReminderDocument
    {

        // assigning null using the null-forgiving operator because the value will always be set
        //[JsonProperty("id")] Newtonsoft no longer used
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        public required ReminderDefinition Reminder { get; set; }
        public long InstallationId { get; set; }
        public long RepositoryId { get; set; }
        public DateTime LastReminder { get; set; }
        public DateTime? NextReminder { get; set; }
        public string Path { get; set; } = null!;

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
    }
}