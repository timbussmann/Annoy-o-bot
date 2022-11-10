using System.Text.Json;
using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Octokit;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_adding_new_reminder_on_non_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_only_create_successful_check_run_for_valid_reminder_definition()
    {
        var commit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Added = new[]
            {
                ".reminders/test.json"
            }
        };
        var callback = CreateGitHubCallbackModel(commits: commit);
        callback.Ref = "my-branch";
        var request = CreateGitHubCallbackRequest(callback);

        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };
        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        FakeReminderCollection documents = new FakeReminderCollection();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), new FakeCosmosWrapper(callback.Installation.Id, callback.Repository.Id));
        var result = await handler.Run(request, documents, null!, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Empty(documents.AddedDocuments);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        Assert.Empty(appInstallation.Comments);

        var checkRun = Assert.Single(appInstallation.CheckRuns);
        Assert.Equal("annoy-o-bot", checkRun.Name);
        Assert.Equal(commit.Id, checkRun.HeadSha);
        Assert.Equal(CheckStatus.Completed, checkRun.Status);
        Assert.Equal(CheckConclusion.Success, checkRun.Conclusion);
    }

    [Fact]
    public async Task Should_only_create_failed_check_run_for_invalid_reminder_definition()
    {
        var commit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Added = new[]
            {
                ".reminders/test.json"
            }
        };
        var callback = CreateGitHubCallbackModel(commits: commit);
        callback.Ref = "my-branch";
        var request = CreateGitHubCallbackRequest(callback);

        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Added[0], "Invalid reminder definition");

        FakeReminderCollection documents = new FakeReminderCollection();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), new FakeCosmosWrapper(callback.Installation.Id, callback.Repository.Id));

        await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, documents, null!, NullLogger.Instance));

        Assert.Empty(documents.AddedDocuments);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        Assert.Empty(appInstallation.Comments);

        var checkRun = Assert.Single(appInstallation.CheckRuns);
        Assert.Equal("annoy-o-bot", checkRun.Name);
        Assert.Equal(commit.Id, checkRun.HeadSha);
        Assert.Equal(CheckStatus.Completed, checkRun.Status);
        Assert.Equal(CheckConclusion.Failure, checkRun.Conclusion);
        Assert.Contains("Invalid reminder definition", checkRun.Output.Title);
    }
}