using System.Text.Json;
using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_deleting_reminder_on_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_delete_reminder_in_database()
    {
        var appInstallation = new FakeGithubInstallation();
        var cosmosDB = new CosmosClientWrapper();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), cosmosDB);

        // Create reminder:
        var createCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Added = new[]
            {
                ".reminders/test.json"
            }
        };
        var createCallback = CreateGitHubCallbackModel(commits: createCommit);
        var createRequest = CreateGitHubCallbackRequest(createCallback);

        var reminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        appInstallation.AddFileContent(createCallback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        await handler.Run(createRequest, documentClient, NullLogger.Instance);

        // Delete reminder:
        var deleteCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Removed = new[]
            {
                ".reminders/test.json"
            }
        };
        var deleteCallback = CreateGitHubCallbackModel(commits: deleteCommit);
        deleteCallback.Installation.Id = createCallback.Installation.Id;
        deleteCallback.Repository.Id = createCallback.Repository.Id;
        var deleteRequest = CreateGitHubCallbackRequest(deleteCallback);

        var result = await handler.Run(deleteRequest, documentClient, NullLogger.Instance);
        
        Assert.IsType<OkResult>(result);

        Assert.Equal(deleteCallback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(deleteCallback.Repository.Id, appInstallation.RepositoryId);

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId).Where(g => g.Key == deleteCommit.Id));
        Assert.Equal(deleteCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Deleted reminder '{deleteCommit.Removed[0]}'", comment.comment);

        await CreateDueReminders(cosmosDB, appInstallation);
        Assert.Empty(appInstallation.Issues);
    }

    public When_deleting_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}