using System;
using Newtonsoft.Json;

namespace Annoy_o_Bot.Parser;

public class JsonReminderParser : ReminderParser
{
    public override ReminderDefinition Parse(string documentContent)
    {
        var reminder = JsonConvert.DeserializeObject<ReminderDefinition>(documentContent);

        if (string.IsNullOrWhiteSpace(reminder.Title))
        {
            throw new ArgumentException("A reminder must provide a non-empty Title property");
        }
        return reminder;
    }
}