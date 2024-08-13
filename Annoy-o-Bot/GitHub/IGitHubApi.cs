using System.Threading.Tasks;
using Annoy_o_Bot.GitHub.Callbacks;
using Microsoft.AspNetCore.Http;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubApi
{
    Task<IGitHubInstallation> GetInstallation(long installationId);
    Task<IGitHubRepository> GetRepository(long installationId, long repositoryId);
}