using Annoy_o_Bot.GitHub;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

class FakeGitHubApi : IGitHubApi
{
    private Dictionary<(long, long), IGitHubRepository> registeredRepos = new();

    public FakeGitHubApi()
    {
    }

    public FakeGitHubApi(params FakeGitHubRepository[] reposInstallations)
    {
        foreach (var installation in reposInstallations)
        {
            AddRepository(installation);
        }
    }

    public void AddRepository(FakeGitHubRepository repository)
    {
        registeredRepos.Add((repository.InstallationId, repository.RepositoryId), repository);
    }

    public Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        return Task.FromResult(registeredRepos[(installationId, repositoryId)]);
    }
}