using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: Contract test for X-GitHub-Event

public class When_callback_type_not_push : CallbackHandlerTest
{
    [Fact]
    public async Task Should_ignore_request()
    {
        var callback = CreateGitHubCallbackModel();
        var request = CreateGitHubCallbackRequest(callback);
        request.Headers["X-GitHub-Event"] = "yolo";

        var appInstallation = new FakeGithubInstallation();

        var handler = new CallbackHandler(appInstallation, configurationBuilder.Build(), new CosmosClientWrapper());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);

        Assert.False(appInstallation.Initialized);
    }

    public When_callback_type_not_push(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}