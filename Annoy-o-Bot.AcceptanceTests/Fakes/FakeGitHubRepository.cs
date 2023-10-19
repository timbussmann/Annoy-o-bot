using Annoy_o_Bot.GitHub;
using Octokit;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

class FakeGitHubRepository : IGitHubRepository
{
    public long InstallationId { get; private set; }

    public long RepositoryId { get; private set; }

    public List<(string commitId, string comment)> Comments { get; set; } = new();

    public List<NewCheckRun> CheckRuns { get; set; } = new();

    public List<NewIssue> Issues { get; set; } = new();

    private readonly Dictionary<string, string> files = new();

    public static FakeGitHubRepository CreateNew()
    {
        return new FakeGitHubRepository(Random.Shared.NextInt64(), Random.Shared.NextInt64());
    }

    public FakeGitHubRepository(long installationId, long repositoryId)
    {
        InstallationId = installationId;
        RepositoryId = repositoryId;
    }

    public void AddFileContent(string filePath, string content)
    {
        files[filePath] = content;
    }

    //TODO test behavior when file does not exist
    public Task<string> ReadFileContent(string filePath, string branchReference)
    {
        return Task.FromResult(files[filePath]);
    }

    public Task CreateCheckRun(NewCheckRun checkRun)
    {
        CheckRuns.Add(checkRun);
        return Task.CompletedTask;
    }

    public Task CreateComment(string commitId, string comment)
    {
        Comments.Add((commitId, comment));
        return Task.CompletedTask;
    }

    public Task<Issue> CreateIssue(NewIssue issue)
    {
        Issues.Add(issue);
        return Task.FromResult(new Issue());
    }


}