using System;

namespace Annoy_o_Bot
{
    public record ReminderDefinition
    {
        public required string Title { get; init; }
        public string? Message { get; init; }
        public string? Assignee { get; init; }
        public string[] Labels { get; init; } = Array.Empty<string>();
        public required DateTime Date { get; init; }
        public required Interval Interval { get; init; }
        public int? IntervalStep { get; init; }
    }

    public enum Interval
    {
        Once = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }
}