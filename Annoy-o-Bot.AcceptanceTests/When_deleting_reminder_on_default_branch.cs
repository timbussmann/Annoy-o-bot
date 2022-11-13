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
        var commit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Removed = new[]
            {
                ".reminders/test.json"
            }
        };
        var callback = CreateGitHubCallbackModel(commits: commit);
        var request = CreateGitHubCallbackRequest(callback);


        var reminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };
        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Removed[0], JsonSerializer.Serialize(reminder));

        var cosmosDB = new CosmosClientWrapper();
        var storedReminder = new ReminderDocument
        {
            InstallationId = callback.Installation.Id,
            RepositoryId = callback.Repository.Id,
            Path = commit.Removed[0],
            LastReminder = DateTime.UtcNow.AddYears(-5),
            NextReminder = DateTime.UtcNow.AddYears(5),
            Reminder = new Reminder()
        };
        await cosmosDB.AddOrUpdateReminder(documentClient, storedReminder);

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), cosmosDB);
        var result = await handler.Run(request, documentClient, NullLogger.Instance);
        
        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        Assert.Null(await cosmosDB.LoadReminder(documentClient, commit.Removed[0], callback.Installation.Id, callback.Repository.Id));

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId));
        Assert.Equal(commit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Deleted reminder '{commit.Removed[0]}'", comment.comment);
    }

    public When_deleting_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}