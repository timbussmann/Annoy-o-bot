using System.Threading.Tasks;

namespace Annoy_o_Bot.GitHub.Api;

public interface IGitHubInstallation
{
    Task<IGitHubRepository> GetRepository(long repositoryId);
}