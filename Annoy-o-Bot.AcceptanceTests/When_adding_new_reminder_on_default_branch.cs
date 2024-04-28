using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: Due to the current implementation, there is very little control over the query time, making it impossible to test various behaviors regarding the interval configuration. The TimeoutFunction needs to be refactored to allow faking the clock for such tests.
public class When_adding_new_reminder_on_default_branch : AcceptanceTest
{
    [Fact]
    public async Task Should_create_reminder_when_due()
    {
        var gitHubApi = new FakeGitHubApi();

        var repository = gitHubApi.CreateNewRepository();
        var reminder = new ReminderDefinition
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);
        var result = await handler.Run(request, container);

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

    [Fact]
    public async Task Should_not_yet_create_reminder_when_not_due()
    {
        var gitHubApi = new FakeGitHubApi();

        var repository = gitHubApi.CreateNewRepository();
        var reminder = new ReminderDefinition
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(5),
            Interval = Interval.Weekly
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);
        var result = await handler.Run(request, container);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, repository.InstallationId);
        Assert.Equal(callback.Repository.Id, repository.RepositoryId);

        await CreateDueReminders(gitHubApi);

        Assert.Empty(repository.Issues);

        var comments = Assert.Single(repository.Comments.GroupBy(c => c.commitId));
        Assert.Equal(callback.HeadCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Created reminder '{reminder.Title}'", comment.comment);
    }

    public When_adding_new_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}