using Annoy_o_Bot.GitHub.Callbacks;
using Newtonsoft.Json;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

static class RepoHelperExtensions
{
    private const string DefaultBranch = "main";

    public static GitPushCallbackModel CommitNewReminder(this FakeGitHubRepository repo, ReminderDefinition reminderDefinition,
        string? branch = null)
    {
        var filename = Guid.NewGuid().ToString("N");

        var commit = CallbackModelHelper.CreateCommitModel(added: $".reminders/{filename}.json");

        repo.AddJsonReminder(commit.Added[0], reminderDefinition);
        return repo.Commit(commit, branch);
    }

    public static GitPushCallbackModel Commit(this FakeGitHubRepository repo, GitPushCallbackModel.CommitModel commit,
        string? branch = null)
    {
        return new GitPushCallbackModel
        {
            Installation = new GitPushCallbackModel.InstallationModel() { Id = repo.InstallationId },
            Repository = new GitPushCallbackModel.RepositoryModel() { Id = repo.RepositoryId, DefaultBranch = DefaultBranch },
            Ref = $"refs/heads/{branch ?? DefaultBranch}",
            HeadCommit = commit,
            Commits = new []{ commit }
        };
    }

    public static void AddJsonReminder(this FakeGitHubRepository repo, string filePath, ReminderDefinition reminderDefinition)
    {
        repo.AddFileContent(filePath, JsonConvert.SerializeObject(reminderDefinition));
    }

}