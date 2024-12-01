using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot.GitHub.Api;

public class GitHubRepository(GitHubClient gitHubClient, long repositoryId, ILogger<GitHubRepository> logger)
    : IGitHubRepository
{
    public async Task<IList<string>> ReadAllRemindersFromDefaultBranch()
    {
        try
        {
            var reminders = await gitHubClient.Repository.Content.GetAllContents(repositoryId, ".reminders");
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
            contents = await gitHubClient.Repository.Content.GetAllContentsByRef(
                repositoryId,
                filePath);
        }
        else
        {
            contents = await gitHubClient.Repository.Content.GetAllContentsByRef(
                repositoryId,
                filePath,
                branchReference);
        }

        return contents.First().Content;
    }

    public async Task CreateCheckRun(NewCheckRun checkRun)
    {
        try
        {
            await gitHubClient.Check.Run.Create(repositoryId, checkRun);
        }
        catch (Exception e)
        {
            // Ignore check run failures for now. Check run permissions were added later, so users might not have granted permissions to add check runs.
            logger.LogWarning(e, $"Failed to create check run for repository {repositoryId}.");
        }
    }

    public Task CreateComment(string commitId, string comment)
    {
        return gitHubClient.Repository.Comment.Create(
            repositoryId,
            commitId,
            new NewCommitComment(comment));
    }

    public Task<Issue> CreateIssue(NewIssue issue)
    {
        return gitHubClient.Issue.Create(repositoryId, issue);
    }
}