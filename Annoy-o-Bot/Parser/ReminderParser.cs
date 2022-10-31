using System;
using System.IO;

namespace Annoy_o_Bot.Parser
{
    public abstract class ReminderParser
    {
        static readonly Lazy<JsonReminderParser> JsonReminderParser = new(() => new JsonReminderParser());
        static readonly Lazy<YamlReminderParser> YamlReminderParser = new(() => new YamlReminderParser());

        public static ReminderParser? GetParser(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".json" => JsonReminderParser.Value,
                ".yaml" or ".yml" => YamlReminderParser.Value,
                _ => null
            };
        }

        public abstract Reminder Parse(string documentContent);
    }
}