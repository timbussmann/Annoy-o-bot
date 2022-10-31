using System;

namespace Annoy_o_Bot.Parser
{
    public abstract class ReminderParser
    {
        static readonly Lazy<JsonReminderParser> JsonReminderParser = new(() => new JsonReminderParser());
        static readonly Lazy<YamlReminderParser> YamlReminderParser = new(() => new YamlReminderParser());

        public static ReminderParser? GetParser(string filePath)
        {
            if (filePath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                return JsonReminderParser.Value;
            }

            if (filePath.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase))
            {
                return YamlReminderParser.Value;
            }

            if (filePath.EndsWith(".yml", StringComparison.InvariantCultureIgnoreCase))
            {
                return YamlReminderParser.Value;
            }

            return null;
        }

        public abstract Reminder Parse(string documentContent);
    }
}