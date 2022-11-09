using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TOOD: Contract test for X-GitHub-Event

public class When_callback_type_not_push : CallbackHandlerTest
{
    [Fact]
    public async Task Should_ignore_request()
    {
        var callback = CreateGitHubCallbackModel();
        var request = CreateGitHubCallbackRequest(callback);
        request.Headers["X-GitHub-Event"] = "yolo";

        var appInstallation = new FakeGithubInstallation();

        FakeReminderCollection documents = new FakeReminderCollection();
        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), null!);
        var result = await handler.Run(request, documents, null!, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.False(appInstallation.Initialized);

        Assert.Empty(documents.AddedDocuments);
    }
}