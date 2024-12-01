using System.Threading.Tasks;

namespace Annoy_o_Bot.GitHub.Api;

public interface IGitHubApi
{
    Task<IGitHubInstallation> GetInstallation(long installationId);
    Task<IGitHubRepository> GetRepository(long installationId, long repositoryId);
}