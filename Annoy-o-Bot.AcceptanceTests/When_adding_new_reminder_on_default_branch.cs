using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_adding_new_reminder_on_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_store_reminder_in_database()
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
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };
        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        var cosmosWrapper = new CosmosClientWrapper();

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), cosmosWrapper);
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        var addedReminder = await cosmosWrapper.LoadReminder(documentClient, commit.Added[0], callback.Installation.Id,
            callback.Repository.Id);
        Assert.NotNull(addedReminder);
        Assert.Equal(callback.Installation.Id, addedReminder.InstallationId);
        Assert.Equal(callback.Repository.Id, addedReminder.RepositoryId);
        Assert.Equal(callback.Commits[0].Added[0], addedReminder.Path);
        Assert.Equal(DateTime.MinValue, addedReminder.LastReminder);
        Assert.Equal(reminder.Date, addedReminder.NextReminder);
        Assert.Equivalent(reminder, addedReminder.Reminder);

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId));
        Assert.Equal(commit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Created reminder '{reminder.Title}'", comment.comment);
    }

    public When_adding_new_reminder_on_default_branch(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}