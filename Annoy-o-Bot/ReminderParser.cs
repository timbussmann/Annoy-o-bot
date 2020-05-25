using System;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Annoy_o_Bot
{
    public abstract class ReminderParser
    {
        public abstract Reminder Parse(string documentContent);
    }

    public class JsonReminderParser : ReminderParser
    {
        public override Reminder Parse(string documentContent)
        {
            var reminder = JsonConvert.DeserializeObject<Reminder>(documentContent);

            if (string.IsNullOrWhiteSpace(reminder.Title))
            {
                throw new ArgumentException("A reminder must provide a non-empty Title property");
            }
            return reminder;
        }
    }

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