using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubRepository
{
    Task<IList<string>> ReadAllRemindersFromDefaultBranch();

    Task<string> ReadFileContent(string filePath, string? branchReference = null);

    Task CreateCheckRun(NewCheckRun checkRun);

    Task CreateComment(string commitId, string comment);

    Task<Issue> CreateIssue(NewIssue issue);
}