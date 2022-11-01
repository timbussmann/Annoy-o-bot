using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

//TODO initialize repo id and remove from method params
public class GitHubAppInstallation : IGitHubAppInstallation
{
    private GitHubClient installationClient = null!;

    public async Task Initialize(long installationId)
    {
        installationClient = await GitHubHelper.GetInstallationClient(installationId);
    }

    public async Task<string> ReadFileContent(string filePath, long repositoryId, string branchReference)
    {
        var contents = await installationClient.Repository.Content.GetAllContentsByRef(
            repositoryId,
            filePath,
            branchReference);

        return contents.First().Content;
    }

    public Task CreateCheckRun(NewCheckRun checkRun, long repositoryId)
    {
        return installationClient.Check.Run.Create(repositoryId, checkRun);
    }

    public Task CreateComment(long repositoryId, string commitId, string comment)
    {
        return installationClient.Repository.Comment.Create(
            repositoryId,
            commitId,
            new NewCommitComment(comment));
    }
}