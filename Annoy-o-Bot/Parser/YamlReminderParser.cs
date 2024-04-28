using System;
using YamlDotNet.Serialization;

namespace Annoy_o_Bot.Parser;

public class YamlReminderParser : ReminderParser
{
    public override ReminderDefinition Parse(string documentContent)
    {
        var deserializer = new Deserializer();
        var reminder = deserializer.Deserialize<ReminderDefinition>(documentContent);

        if (string.IsNullOrWhiteSpace(reminder.Title))
        {
            throw new ArgumentException("A reminder must provide a non-empty Title property");
        }
        return reminder;
    }
}