using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: When creating reminder on non-default branch
//TODO: When updating reminder on non-default branch
//TODO: When deleting reminder on non-default branch
//TODO: When github event is not push

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

        FakeReminderCollection documents = new FakeReminderCollection();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), new FakeCosmosWrapper(callback.Installation.Id, callback.Repository.Id));
        var result = await handler.Run(request, documents, null!, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.Equal(callback.Installation.Id, appInstallation.InstallationId);
        Assert.Equal(callback.Repository.Id, appInstallation.RepositoryId);

        var addedReminder = Assert.Single(documents.AddedDocuments);
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
}