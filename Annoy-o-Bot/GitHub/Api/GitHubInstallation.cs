using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub.Api;

class GitHubInstallation : IGitHubInstallation
{
    readonly ILoggerFactory loggerFactory;
    readonly GitHubClient installationClient;
    readonly long installationId;

    public GitHubInstallation(GitHubClient installationClient, long installationId, ILoggerFactory loggerFactory)
    {
        this.installationClient = installationClient;
        this.installationId = installationId;
        this.loggerFactory = loggerFactory;
    }

    public Task<IGitHubRepository> GetRepository(long repositoryId)
    {
        return Task.FromResult<IGitHubRepository>(new GitHubRepository(installationClient, repositoryId, loggerFactory.CreateLogger<GitHubRepository>()));
    }
}