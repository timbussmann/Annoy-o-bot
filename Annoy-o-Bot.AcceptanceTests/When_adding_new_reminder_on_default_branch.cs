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
        var repository = FakeGitHubRepository.CreateNew();
        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var gitHubApi = new FakeGitHubApi(repository);
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, repository.InstallationId);
        Assert.Equal(callback.Repository.Id, repository.RepositoryId);

        await CreateDueReminders(gitHubApi);

        var issue = Assert.Single(repository.Issues);
        Assert.Equal(reminder.Title, issue.Title);
        Assert.Equal(reminder.Message, issue.Body);

        var comments = Assert.Single(repository.Comments.GroupBy(c => c.commitId));
        Assert.Equal(callback.HeadCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Created reminder '{reminder.Title}'", comment.comment);
    }

    public When_adding_new_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}