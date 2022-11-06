using System.Net.Sockets;
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

public class When_adding_new_reminder_to_default_branch
{
    private const string SignatureKey = "mysecretkey";

    [Fact]
    public async Task Should_store_reminder_in_database()
    {
        var headCommit = new CallbackModel.CommitModel
        {
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

        var reminder = new Reminder()
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(10),
            Interval = Interval.Weekly
        };

        HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext());
        var messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(callback, Formatting.None));
        request.Body = new MemoryStream( messageContent);
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256", "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SignatureKey), messageContent)));

        FakeReminderCollection documents = new FakeReminderCollection();

        var appInstallation = new FakeGithubInstallation();
        appInstallation.AddFileContent(callback.Commits[0].Added[0], JsonSerializer.Serialize(reminder));

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
           { "WebhookSecret", SignatureKey }
        });

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build());
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