﻿using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_assigning_users_to_reminder : AcceptanceTest
{
    [Fact]
    public async Task Should_assign_issue_to_single_assignee()
    {
        const string Assignee = "testuser";

        var repository = FakeGitHubRepository.CreateNew();
        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly,
            Assignee = Assignee
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var gitHubApi = new FakeGitHubApi(repository);
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);
        await CreateDueReminders(gitHubApi);

        var issue = Assert.Single(repository.Issues);
        Assert.Single(issue.Assignees, Assignee);
    }

    [Fact]
    public async Task Should_assign_issue_to_multiple_assignees()
    {
        var repository = FakeGitHubRepository.CreateNew();
        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly,
            Assignee = "User1;User2;User3;"
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var gitHubApi = new FakeGitHubApi(repository);
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);
        await CreateDueReminders(gitHubApi);

        var issue = Assert.Single(repository.Issues);
        Assert.Contains("User1", issue.Assignees);
        Assert.Contains("User2", issue.Assignees);
        Assert.Contains("User3", issue.Assignees);
        Assert.Equal(3, issue.Assignees.Count);
    }

    public When_assigning_users_to_reminder(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}