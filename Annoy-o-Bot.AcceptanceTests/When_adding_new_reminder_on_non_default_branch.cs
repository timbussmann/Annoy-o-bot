﻿using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Octokit;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_adding_new_reminder_on_non_default_branch : AcceptanceTest
{
    [Fact]
    public async Task Should_only_create_successful_check_run_for_valid_reminder_definition()
    {
        var gitHubApi = new FakeGitHubApi();

        var appInstallation = gitHubApi.CreateNewRepository();
        var reminder = new ReminderDefinition
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly,
        };
        var callback = appInstallation.CommitNewReminder(reminder, branch: "my-branch");
        var request = CreateCallbackHttpRequest(callback);

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        var result = await handler.Run(request, container);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        Assert.Empty(appInstallation.Comments);

        var checkRun = Assert.Single(appInstallation.CheckRuns);
        Assert.Equal("annoy-o-bot", checkRun.Name);
        Assert.Equal(callback.HeadCommit.Id, checkRun.HeadSha);
        Assert.Equal(CheckStatus.Completed, checkRun.Status);
        Assert.Equal(CheckConclusion.Success, checkRun.Conclusion);

        await CreateDueReminders(gitHubApi);

        Assert.Empty(appInstallation.Issues);
    }

    [Fact]
    public async Task Should_only_create_failed_check_run_for_invalid_reminder_definition()
    {
        var gitHubApi = new FakeGitHubApi();
        var appInstallation = gitHubApi.CreateNewRepository();

        var callback = appInstallation.CommitNewReminder(new ReminderDefinition
        {
            Title = null!,
            Date = default,
            Interval = Interval.Once
        }, branch: "my-branch");
        var request = CreateCallbackHttpRequest(callback);
        appInstallation.AddFileContent(callback.Commits[0].Added[0], "Invalid reminder definition");

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, container));

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        Assert.Empty(appInstallation.Comments);

        var checkRun = Assert.Single(appInstallation.CheckRuns);
        Assert.Equal("annoy-o-bot", checkRun.Name);
        Assert.Equal(callback.HeadCommit.Id, checkRun.HeadSha);
        Assert.Equal(CheckStatus.Completed, checkRun.Status);
        Assert.Equal(CheckConclusion.Failure, checkRun.Conclusion);
        Assert.Contains("Invalid reminder definition", checkRun.Output.Title);

        await CreateDueReminders(gitHubApi);

        Assert.Empty(appInstallation.Issues);
    }

    public When_adding_new_reminder_on_non_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}