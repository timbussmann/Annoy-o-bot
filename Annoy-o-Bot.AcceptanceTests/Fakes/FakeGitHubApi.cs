using Annoy_o_Bot.GitHub;
using Annoy_o_Bot.GitHub.Callbacks;
using Microsoft.AspNetCore.Http;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

class FakeGitHubApi : IGitHubApi
{
    private Dictionary<(long, long), IGitHubRepository> registeredRepos = new();

    public FakeGitHubApi()
    {
    }

    public FakeGitHubRepository CreateNewRepository()
    {
        var repository = new FakeGitHubRepository(Random.Shared.NextInt64(), Random.Shared.NextInt64());
        registeredRepos.Add((repository.InstallationId, repository.RepositoryId), repository);
        return repository;
    }

    public Task<IGitHubInstallation> GetInstallation(long installationId)
    {
        return Task.FromResult<IGitHubInstallation>(new FakeGitHubInstallation(this, installationId));
    }

    public Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        return Task.FromResult(registeredRepos[(installationId, repositoryId)]);
    }

    public async Task<CallbackModel> ValidateCallback(HttpRequest callbackRequest, string secret)
    {
        var content = await new StreamReader(callbackRequest.Body).ReadToEndAsync();
        return RequestParser.ParseJson(content);
    }
}