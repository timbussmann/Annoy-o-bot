using System.Text.Json;
using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_request_signature_does_not_match : CallbackHandlerTest
{
    [Fact]
    public async Task Should_return_error()
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
        var requestHash = request.Headers["X-Hub-Signature-256"];
        request.Headers["X-Hub-Signature-256"] = "1234567890"; // this is not the incorrect but the expected signature in this test

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

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, documents, null!, NullLogger.Instance));

        Assert.Contains(
            $"Computed request payload signature ('{requestHash}') does not match provided signature ('{request.Headers["X-Hub-Signature-256"]}')",
            exception.Message);
        
        Assert.False(appInstallation.Initialized);

        Assert.Empty(documents.AddedDocuments);
    }
}