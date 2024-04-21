using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public class GitHubRepository : IGitHubRepository
{
    private readonly GitHubClient installationClient;
    private readonly long repositoryId;
    readonly ILogger<GitHubRepository> logger;

    public GitHubRepository(GitHubClient gitHubClient, long repositoryId, ILogger<GitHubRepository> logger)
    {
        this.repositoryId = repositoryId;
        this.logger = logger;
        installationClient = gitHubClient;
    }

    public async Task<IList<(string path, string content)>> ReadAllRemindersFromDefaultBranch()
    {
        try
        {
            var reminders = await installationClient.Repository.Content.GetAllContents(repositoryId, ".reminders");
            return reminders.Select(content => (content.Path, content.Content)).ToList();
        }
        catch (NotFoundException e)
        {
            logger.LogWarning("Couldn't find reminders folder for repository {repository}", repositoryId);
            return new List<(string path, string content)>(0);
        }
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