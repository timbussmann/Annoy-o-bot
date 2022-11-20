using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: Contract test for X-GitHub-Event

public class When_callback_type_not_push : AcceptanceTest
{
    [Fact]
    public async Task Should_ignore_request()
    {
        var callback = CreateGitHubCallbackModel();
        var request = CreateGitHubCallbackRequest(callback);
        request.Headers["X-GitHub-Event"] = "yolo";

        var handler = new CallbackHandler(new FakeGitHubApi(), configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);
    }

    public When_callback_type_not_push(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}