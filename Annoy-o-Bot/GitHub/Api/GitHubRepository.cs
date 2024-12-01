using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub.Api;

public class GitHubRepository : IGitHubRepository
{
    readonly GitHubClient installationClient;
    readonly long repositoryId;
    readonly ILogger<GitHubRepository> logger;

    public GitHubRepository(GitHubClient gitHubClient, long repositoryId, ILogger<GitHubRepository> logger)
    {
        this.repositoryId = repositoryId;
        this.logger = logger;
        installationClient = gitHubClient;
    }

    public async Task<IList<string>> ReadAllRemindersFromDefaultBranch()
    {
        try
        {
            var reminders = await installationClient.Repository.Content.GetAllContents(repositoryId, ".reminders");
            return reminders.Select(content => content.Path).ToList();
        }
        catch (NotFoundException e)
        {
            logger.LogWarning("Couldn't find reminders folder for repository {repository}", repositoryId);
            return new List<string>(0);
        }
    }

    public async Task<string> ReadFileContent(string filePath, string? branchReference)
    {
        IReadOnlyList<RepositoryContent> contents;

        if (branchReference == null)
        {
            contents = await installationClient.Repository.Content.GetAllContentsByRef(
                repositoryId,
                filePath);
        }
        else
        {
            contents = await installationClient.Repository.Content.GetAllContentsByRef(
                repositoryId,
                filePath,
                branchReference);
        }

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