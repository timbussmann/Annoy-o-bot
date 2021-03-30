using System;

namespace Annoy_o_Bot
{
    public class Reminder
    {
        public Reminder()
        {
            Labels = Array.Empty<string>();
        }

        public string Title { get; set; } = null!;
        public string? Message { get; set; }
        public string? Assignee { get; set; }
        public string[] Labels { get; set; }
        public DateTime Date { get; set; }
        public Interval Interval { get; set; }
        public int? IntervalStep { get; set; }
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