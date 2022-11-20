using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

//TODO: Contract test for X-Hub-Signature-256

public class When_request_signature_does_not_match : AcceptanceTest
{
    [Fact]
    public async Task Should_return_error()
    {
        var repository = FakeGitHubRepository.CreateNew();
        var request = CreateCallbackHttpRequest(repository.Commit(CallbackModelHelper.CreateCommitModel()));
        var requestHash = request.Headers["X-Hub-Signature-256"];
        request.Headers["X-Hub-Signature-256"] = "1234567890"; // this is not the incorrect but the expected signature in this test

        var handler = new CallbackHandler(new FakeGitHubApi(), configurationBuilder.Build());

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, documentClient, NullLogger.Instance));

        Assert.Contains(
            $"Computed request payload signature ('{requestHash}') does not match provided signature ('{request.Headers["X-Hub-Signature-256"]}')",
            exception.Message);
    }

    public When_request_signature_does_not_match(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}