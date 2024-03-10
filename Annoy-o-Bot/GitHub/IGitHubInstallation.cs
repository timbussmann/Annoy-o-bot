using System.Threading.Tasks;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubInstallation
{
    Task<IGitHubRepository> GetRepository(long repositoryId);
}