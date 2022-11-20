using System.Threading.Tasks;

namespace Annoy_o_Bot.GitHub;

public class GitHubApi : IGitHubApi
{
    public async Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        var installationClient = await GitHubHelper.GetInstallationClient(installationId);
        return new GitHubRepository(installationClient, repositoryId);
    }
}