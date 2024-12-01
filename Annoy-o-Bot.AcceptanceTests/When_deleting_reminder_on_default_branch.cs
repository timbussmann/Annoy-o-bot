using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_deleting_reminder_on_default_branch : AcceptanceTest
{
    [Fact]
    public async Task Should_delete_reminder_in_database()
    {
        var gitHubApi = new FakeGitHubApi();
        var repository = gitHubApi.CreateNewRepository();
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        // Create reminder:
        var reminder = new ReminderDefinition
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        var createCallback = repository.CommitNewReminder(reminder);
        var createRequest = CreateCallbackHttpRequest(createCallback);

        await handler.Run(createRequest, container);

        // Delete reminder:
        var deleteCommit = CallbackModelHelper.CreateCommitModel(removed: createCallback.HeadCommit.Added[0]);
        var deleteCallback = repository.Commit(deleteCommit);
        var deleteRequest = CreateCallbackHttpRequest(deleteCallback);

        var result = await handler.Run(deleteRequest, container);
        
        Assert.IsType<OkResult>(result);

        Assert.Equal(deleteCallback.Installation.Id, repository.InstallationId);
        Assert.Equal(deleteCallback.Repository.Id, repository.RepositoryId);

        var comments = Assert.Single(repository.Comments.GroupBy(c => c.commitId).Where(g => g.Key == deleteCommit.Id));
        Assert.Equal(deleteCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Deleted reminder '{deleteCommit.Removed[0]}'", comment.comment);

        await CreateDueReminders(gitHubApi);
        Assert.Empty(repository.Issues);
    }

    public When_deleting_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}