using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubAppInstallation
{
    Task Initialize(long installationId, long repositoryId);

    Task<string> ReadFileContent(string filePath, string branchReference);

    Task CreateCheckRun(NewCheckRun checkRun);
    Task CreateComment(string commitId, string comment);
}