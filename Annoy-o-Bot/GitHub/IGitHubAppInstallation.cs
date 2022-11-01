using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubAppInstallation
{
    Task Initialize(long installationId);

    Task<string> ReadFileContent(string filePath, long repositoryId, string branchReference);

    Task CreateCheckRun(NewCheckRun checkRun, long repositoryId);
    Task CreateComment(long repositoryId, string commitId, string comment);
}