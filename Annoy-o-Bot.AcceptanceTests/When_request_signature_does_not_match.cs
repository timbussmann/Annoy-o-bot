using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_request_signature_does_not_match : AcceptanceTest
{
    [Fact]
    public async Task Should_return_error()
    {
        const string InvalidSignature = "sha256=e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        var gitHubApi = new FakeGitHubApi();
        var repository = gitHubApi.CreateNewRepository();

        var request = CreateCallbackHttpRequest(repository.Commit(CallbackModelHelper.CreateCommitModel()));
        var requestHash = request.Headers["X-Hub-Signature-256"];
        request.Headers["X-Hub-Signature-256"] = InvalidSignature; // this is not the incorrect but the expected signature in this test

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => handler.Run(request, container));

        Assert.Contains(
            $"Computed request payload signature ('{requestHash}') does not match provided signature ('{InvalidSignature}')",
            exception.Message);
    }

    public When_request_signature_does_not_match(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}