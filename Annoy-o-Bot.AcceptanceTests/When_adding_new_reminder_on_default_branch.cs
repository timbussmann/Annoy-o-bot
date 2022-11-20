using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO Tests for different reminder config options (assignee, labels, ...)
//TODO Tests for different interval configurations

public class When_adding_new_reminder_on_default_branch : AcceptanceTest
{
    [Fact]
    public async Task Should_create_reminder_when_due()
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
        var request = CreateGitHubCallbackRequest(callback);


        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        var appInstallation = new FakeGitHubRepository(callback.Installation.Id, callback.Repository.Id);
        appInstallation.AddFileContent(callback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        var gitHubApi = new FakeGitHubApi(appInstallation);
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        await CreateDueReminders(gitHubApi);

        var issue = Assert.Single(appInstallation.Issues);
        Assert.Equal(reminder.Title, issue.Title);
        Assert.Equal(reminder.Message, issue.Body);

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId));
        Assert.Equal(commit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Created reminder '{reminder.Title}'", comment.comment);
    }

    public When_adding_new_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}