using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: Contract test for X-Hub-Signature-256

public class When_request_signature_does_not_match : CallbackHandlerTest
{
    [Fact]
    public async Task Should_return_error()
    {
        var callback = CreateGitHubCallbackModel();
        var request = CreateGitHubCallbackRequest(callback);
        var requestHash = request.Headers["X-Hub-Signature-256"];
        request.Headers["X-Hub-Signature-256"] = "1234567890"; // this is not the incorrect but the expected signature in this test

        var appInstallation = new FakeGithubInstallation();
        FakeReminderCollection documents = new FakeReminderCollection();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), null!);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, documents, null!, NullLogger.Instance));

        Assert.Contains(
            $"Computed request payload signature ('{requestHash}') does not match provided signature ('{request.Headers["X-Hub-Signature-256"]}')",
            exception.Message);
        
        Assert.False(appInstallation.Initialized);

        Assert.Empty(documents.AddedDocuments);
    }
}