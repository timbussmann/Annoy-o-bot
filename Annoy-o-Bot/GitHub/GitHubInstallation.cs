using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

class GitHubInstallation : IGitHubInstallation
{
    readonly GitHubClient installationClient;
    readonly long installationId;

    public GitHubInstallation(GitHubClient installationClient, long installationId)
    {
        this.installationClient = installationClient;
        this.installationId = installationId;
    }

    public Task<IGitHubRepository> GetRepository(long repositoryId)
    {
        return Task.FromResult<IGitHubRepository>(new GitHubRepository(installationClient, repositoryId));
    }
}