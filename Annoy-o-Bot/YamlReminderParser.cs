using System;
using YamlDotNet.Serialization;

namespace Annoy_o_Bot
{
    public class YamlReminderParser
    {
        public static Reminder Parse(string documentContent)
        {
            Deserializer deserializer = new Deserializer();
            var reminder = deserializer.Deserialize<Reminder>(documentContent);

            if (string.IsNullOrWhiteSpace(reminder.Title))
            {
                throw new ArgumentException("A reminder must provide a non-empty Title property");
            }
            return reminder;
        }
    }
}