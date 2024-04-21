using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public class GitHubApi : IGitHubApi
{
    ILoggerFactory loggerFactory;

    public GitHubApi(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public async Task<IGitHubInstallation> GetInstallation(long installationId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubInstallation(installationClient, installationId, loggerFactory);
    }

    public async Task<IGitHubRepository> GetRepository(long installationId, long repositoryId)
    {
        var installationClient = await GetInstallationClient(installationId);
        return new GitHubRepository(installationClient, repositoryId, loggerFactory.CreateLogger<GitHubRepository>());
    }

    static Task<GitHubClient> GetInstallationClient(long installationId) => GitHubHelper.GetInstallationClient(installationId);
}