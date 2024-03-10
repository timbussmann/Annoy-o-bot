using System.Text.Json;
using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_updating_reminder_on_default_branch : AcceptanceTest
{
    [Fact]
    public async Task Should_update_reminder_in_database()
    {
        var gitHubApi = new FakeGitHubApi();
        var appInstallation = gitHubApi.CreateNewRepository();
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        // Create reminder:
        var initialReminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddYears(5),
            Interval = Interval.Weekly
        };
        var createCallback = appInstallation.CommitNewReminder(initialReminder);
        var createRequest = CreateCallbackHttpRequest(createCallback);

        await handler.Run(createRequest, container);

        // Update reminder:
        var updatedReminder = new Reminder
        {
            Title = "Updated title for the reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        appInstallation.AddFileContent(createCallback.Commits[0].Added[0], JsonSerializer.Serialize(updatedReminder));

        var updateCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Modified = new[]
            {
                createCallback.HeadCommit.Added[0]
            }
        };
        var updateCallback = appInstallation.Commit(updateCommit);
        var updateRequest = CreateCallbackHttpRequest(updateCallback);
        
        var result = await handler.Run(updateRequest, container);

        Assert.IsType<OkResult>(result);

        Assert.Equal(updateCallback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(updateCallback.Repository.Id, appInstallation.RepositoryId);
        
        var comment = Assert.Single(appInstallation.Comments.Where(c => c.commitId == updateCommit.Id));
        Assert.Contains($"Updated reminder '{updatedReminder.Title}'", comment.comment);

        await CreateDueReminders(gitHubApi);
        var issue = Assert.Single(appInstallation.Issues);
        Assert.Equal(updatedReminder.Title, issue.Title);
    }

    public When_updating_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}