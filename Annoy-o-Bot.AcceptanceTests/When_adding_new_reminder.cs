using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: When deleting reminder on default branch
//TODO: When creating reminder on non-default branch
//TODO: When updating reminder on non-default branch
//TODO: When deleting reminder on non-default branch
//TODO: When signature headers doesn't match with payload signature
//TODO: When github event is not push

public class CallbackHandlerTest
{
    protected const string SignatureKey = "mysecretkey";

    protected ConfigurationBuilder configurationBuilder;

    public CallbackHandlerTest()
    {
        configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "WebhookSecret", SignatureKey }
        });
    }

    protected static HttpRequest CreateGitHubCallback(CallbackModel callback)
    {
        HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext());
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(callback, Formatting.None));
        request.Body = new MemoryStream(messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256",
            "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));
        return request;
    }
}

public class When_updating_reminder_on_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_update_reminder_in_database()
    {
        var headCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Modified = new[]
            {
                ".reminders/test.json"
            }
        };
        var callback = new CallbackModel
        {
            Installation = new CallbackModel.InstallationModel() { Id = Random.Shared.NextInt64() },
            Repository = new CallbackModel.RepositoryModel() { Id = Random.Shared.NextInt64(), DefaultBranch = "main" },
            Ref = "refs/heads/main",
            HeadCommit = headCommit,
            Commits = new[] { headCommit }
        };

        var reminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };

        var request = CreateGitHubCallback(callback);

        FakeReminderCollection documents = new FakeReminderCollection();

        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Modified[0], JsonSerializer.Serialize(reminder));

        var cosmosDB = new FakeCosmosWrapper(callback.Installation.Id, callback.Repository.Id);
        var storedReminder = cosmosDB.StoredReminders[headCommit.Modified[0]] = new ReminderDocument()
        {
            Id = "existing document id",
            InstallationId = 123,
            RepositoryId = 456,
            Path = "/some/path",
            LastReminder = DateTime.UtcNow.AddYears(-5),
            NextReminder = DateTime.UtcNow.AddYears(5),
            Reminder = new Reminder()
        };

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), cosmosDB);
        var result = await handler.Run(request, documents, null!, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        var addedReminder = Assert.Single(documents.AddedDocuments);
        // Should not update certain properties
        Assert.Equal(storedReminder.InstallationId, addedReminder.InstallationId);
        Assert.Equal(storedReminder.RepositoryId, addedReminder.RepositoryId);
        Assert.Equal(storedReminder.Path, addedReminder.Path);
        Assert.Equal(storedReminder.LastReminder, addedReminder.LastReminder);
        // Should update next reminder date and reminder data based on incoming request
        Assert.Equal(reminder.Date, addedReminder.NextReminder);
        Assert.Equivalent(reminder, addedReminder.Reminder);

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId));
        Assert.Equal(headCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Updated reminder '{reminder.Title}'", comment.comment);
    }
}

public class When_adding_new_reminder_on_default_branch : CallbackHandlerTest
{
    [Fact]
    public async Task Should_store_reminder_in_database()
    {
        var headCommit = new CallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Added = new[]
            {
                ".reminders/test.json"
            }
        };
        var callback = new CallbackModel
        {
            Installation = new CallbackModel.InstallationModel() { Id = Random.Shared.NextInt64() },
            Repository = new CallbackModel.RepositoryModel() { Id = Random.Shared.NextInt64(),  DefaultBranch = "main"},
            Ref = "refs/heads/main",
            HeadCommit = headCommit,
            Commits = new[] { headCommit }
        };

        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };

        var request = CreateGitHubCallback(callback);

        FakeReminderCollection documents = new FakeReminderCollection();

        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), new FakeCosmosWrapper(callback.Installation.Id, callback.Repository.Id));
        var result = await handler.Run(request, documents, null!, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        var addedReminder = Assert.Single(documents.AddedDocuments);
        Assert.Equal(callback.Installation.Id, addedReminder.InstallationId);
        Assert.Equal(callback.Repository.Id, addedReminder.RepositoryId);
        Assert.Equal(callback.Commits[0].Added[0], addedReminder.Path);
        Assert.Equal(DateTime.MinValue, addedReminder.LastReminder);
        Assert.Equal(reminder.Date, addedReminder.NextReminder);
        Assert.Equivalent(reminder, addedReminder.Reminder);

        var comments = Assert.Single(appInstallation.Comments.GroupBy(c => c.commitId));
        Assert.Equal(headCommit.Id, comments.Key);
        var comment = Assert.Single(comments);
        Assert.Contains($"Created reminder '{reminder.Title}'", comment.comment);
    }
}