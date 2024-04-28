using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_updating_reminder_not_stored : AcceptanceTest
{
    [Fact]
    public async Task Should_create_reminder_in_database()
    {
        var gitHubApi = new FakeGitHubApi();
        var appInstallation = gitHubApi.CreateNewRepository();
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        // Update reminder:
        var updatedReminder = new ReminderDefinition
        {
            Title = "Updated title for the reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        var createCallback = appInstallation.CommitNewReminder(updatedReminder);

        var updateCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Modified =
            [
                createCallback.HeadCommit.Added[0]
            ]
        };
        var updateCallback = appInstallation.Commit(updateCommit);
        var updateRequest = CreateCallbackHttpRequest(updateCallback);

        var result = await handler.Run(updateRequest, container);

        Assert.IsType<OkResult>(result);

        Assert.Equal(updateCallback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(updateCallback.Repository.Id, appInstallation.RepositoryId);

        var cosmosWrapper = new CosmosClientWrapper(container);
        var reminderDocument = await cosmosWrapper.LoadReminder(updateCallback.HeadCommit.Modified[0],
            updateCallback.Installation.Id, updateCallback.Repository.Id);
        Assert.NotNull(reminderDocument);
        Assert.Equal(DateTime.MinValue, reminderDocument.LastReminder);

        var comment = Assert.Single(appInstallation.Comments.Where(c => c.commitId == updateCommit.Id));
        Assert.Contains($"Created reminder '{updatedReminder.Title}'", comment.comment);
    }

    public When_updating_reminder_not_stored(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}