using System;
using Newtonsoft.Json;

namespace Annoy_o_Bot
{
    public class ReminderParser
    {
        public static Reminder ParseJson(string documentContent)
        {
            var reminder = JsonConvert.DeserializeObject<Reminder>(documentContent);

            if (string.IsNullOrWhiteSpace(reminder.Title))
            {
                throw new ArgumentException("A reminder must provide a non-empty Title property");
            }
            return reminder;
        }
    }
}