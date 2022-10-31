using System;
using YamlDotNet.Serialization;

namespace Annoy_o_Bot.Parser;

public class YamlReminderParser : ReminderParser
{
    public override Reminder Parse(string documentContent)
    {
        var deserializer = new Deserializer();
        var reminder = deserializer.Deserialize<Reminder>(documentContent);

        if (string.IsNullOrWhiteSpace(reminder.Title))
        {
            throw new ArgumentException("A reminder must provide a non-empty Title property");
        }
        return reminder;
    }
}