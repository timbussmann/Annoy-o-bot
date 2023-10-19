using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_callback_type_not_push : AcceptanceTest
{
    [Fact]
    public async Task Should_ignore_request()
    {
        var repository = FakeGitHubRepository.CreateNew();
        var callback = repository.Commit(null);
        var request = CreateCallbackHttpRequest(callback);
        request.Headers["X-GitHub-Event"] = "yolo";

        var handler = new CallbackHandler(new FakeGitHubApi(), configurationBuilder.Build());
        var result = await handler.Run(request, documentClient, NullLogger.Instance);

        Assert.IsType<OkResult>(result);
    }

    public When_callback_type_not_push(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}