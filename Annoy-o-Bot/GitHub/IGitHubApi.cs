using System.Threading.Tasks;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubApi
{
    Task<IGitHubRepository> GetRepository(long installationId, long repositoryId);
}