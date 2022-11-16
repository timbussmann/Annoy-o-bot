using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public class GitHubAppInstallation : IGitHubAppInstallation
{
    private GitHubClient installationClient = null!;
    private long repositoryId;

    public async Task Initialize(long installationId, long repositoryId)
    {
        this.repositoryId = repositoryId;
        installationClient = await GitHubHelper.GetInstallationClient(installationId);
    }

    public async Task<string> ReadFileContent(string filePath, string branchReference)
    {
        var contents = await installationClient.Repository.Content.GetAllContentsByRef(
            repositoryId,
            filePath,
            branchReference);

        return contents.First().Content;
    }

    public Task CreateCheckRun(NewCheckRun checkRun)
    {
        return installationClient.Check.Run.Create(repositoryId, checkRun);
    }

    public Task CreateComment(string commitId, string comment)
    {
        return installationClient.Repository.Comment.Create(
            repositoryId,
            commitId,
            new NewCommitComment(comment));
    }

    public Task<Issue> CreateIssue(NewIssue issue)
    {
        return installationClient.Issue.Create(repositoryId, issue);
    }
}