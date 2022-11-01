using Annoy_o_Bot.GitHub;
using Octokit;

namespace Annoy_o_Bot.AcceptanceTests;

class FakeGithubInstallation : IGitHubAppInstallation
{
    public bool Initialized { get; private set; }

    public long InstallationId { get; private set; }

    public Task Initialize(long installationId)
    {
        this.InstallationId = installationId;
        this.Initialized = true;
        return Task.CompletedTask;
    }

    private readonly Dictionary<string, string> files = new();
    public void AddFileContent(string filePath, string content)
    {
        this.files.Add(filePath, content);
    }

    //TODO test behavior when file does not exist
    public Task<string> ReadFileContent(string filePath, long repositoryId, string branchReference)
    {
        return Task.FromResult(this.files[filePath]);
    }

    public Task CreateCheckRun(NewCheckRun checkRun, long repositoryId)
    {
        return Task.CompletedTask;
    }

    public Task CreateComment(long repositoryId, string commitId, string comment)
    {
        return Task.CompletedTask;
    }
}