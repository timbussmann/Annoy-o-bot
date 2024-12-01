using Annoy_o_Bot.GitHub.Api;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

class FakeGitHubInstallation : IGitHubInstallation
{
    readonly FakeGitHubApi fakeGitHubApi;
    readonly long installationId;

    public FakeGitHubInstallation(FakeGitHubApi fakeGitHubApi, long installationId)
    {
        this.fakeGitHubApi = fakeGitHubApi;
        this.installationId = installationId;
    }

    public Task<IGitHubRepository> GetRepository(long repositoryId)
    {
        return fakeGitHubApi.GetRepository(installationId, repositoryId);
    }
}