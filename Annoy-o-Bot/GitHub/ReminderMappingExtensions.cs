﻿using Octokit;
using System.Linq;
using System;

namespace Annoy_o_Bot.GitHub;

public static class ReminderMappingExtensions
{
    public static NewIssue ToGitHubIssue(this ReminderDefinition reminderDefinition)
    {
        var newIssue = new NewIssue(reminderDefinition.Title)
        {
            Body = reminderDefinition.Message,
        };
        foreach (var assignee in reminderDefinition.Assignee?.Split(';',
                     StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>())
        {
            newIssue.Assignees.Add(assignee);
        }

        foreach (var label in reminderDefinition.Labels)
        {
            newIssue.Labels.Add(label);
        }

        return newIssue;
    }
}