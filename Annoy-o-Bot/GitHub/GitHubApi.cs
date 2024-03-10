using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public class GitHubApi : IGitHubApi
{
    public async Task<IGitHubInstallation> GetInstallation(long installationId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubInstallation(installationClient, installationId);
    }

    public async Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubRepository(installationClient, repositoryId);
    }

    static Task<GitHubClient> GetInstallationClient(long installationId) => GitHubHelper.GetInstallationClient(installationId);
}