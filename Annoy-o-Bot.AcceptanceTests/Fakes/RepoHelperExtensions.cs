using Newtonsoft.Json;
using Octokit;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

static class RepoHelperExtensions
{
    private const string DefaultBranch = "main";

    public static CallbackModel CommitNewReminder(this FakeGitHubRepository repo, Reminder reminder,
        string? branch = null)
    {
        var filename = Guid.NewGuid().ToString("N");

        var commit = CallbackModelHelper.CreateCommitModel(added: $".reminders/{filename}.json");

        repo.AddJsonReminder(commit.Added[0], reminder);
        return repo.Commit(commit, branch);
    }

    public static CallbackModel Commit(this FakeGitHubRepository repo, CallbackModel.CommitModel commit,
        string? branch = null)
    {
        return new CallbackModel
        {
            Installation = new CallbackModel.InstallationModel() { Id = repo.InstallationId },
            Repository = new CallbackModel.RepositoryModel() { Id = repo.RepositoryId, DefaultBranch = DefaultBranch },
            Ref = $"refs/heads/{branch ?? DefaultBranch}",
            HeadCommit = commit,
            Commits = new []{ commit }
        };
    }

    public static void AddJsonReminder(this FakeGitHubRepository repo, string filePath, Reminder reminder)
    {
        repo.AddFileContent(filePath, JsonConvert.SerializeObject(reminder));
    }

}