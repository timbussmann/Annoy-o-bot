using System.Text.Json;
using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_updating_reminder_on_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_update_reminder_in_database()
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

        var initialReminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddYears(5),
            Interval = Interval.Weekly
        };
        appInstallation.AddFileContent(createCallback.Commits[0].Added[0], JsonSerializer.Serialize(initialReminder));

        await handler.Run(createRequest, documentClient, NullLogger.Instance);

        // Update reminder:
        var updateCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Modified = new[]
            {
                ".reminders/test.json"
            }
        };
        var updateCallback = CreateGitHubCallbackModel(commits: updateCommit);
        updateCallback.Installation.Id = createCallback.Installation.Id;
        updateCallback.Repository.Id = createCallback.Repository.Id;
        var updateRequest = CreateGitHubCallbackRequest(updateCallback);

        var updatedReminder = new Reminder
        {
            Title = "Updated title for the reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        appInstallation.AddFileContent(createCallback.Commits[0].Added[0], JsonSerializer.Serialize(updatedReminder));

        var result = await handler.Run(updateRequest, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Equal(updateCallback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(updateCallback.Repository.Id, appInstallation.RepositoryId);
        
        var comment = Assert.Single(appInstallation.Comments.Where(c => c.commitId == updateCommit.Id));
        Assert.Contains($"Updated reminder '{updatedReminder.Title}'", comment.comment);

        await CreateDueReminders(cosmosDB, appInstallation);
        var issue = Assert.Single(appInstallation.Issues);
        Assert.Equal(updatedReminder.Title, issue.Title);
    }

    public When_updating_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}