using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace Annoy_o_Bot.GitHub;

public interface IGitHubRepository
{
    Task<IList<(string path, string content)>> ReadAllRemindersFromDefaultBranch();

    Task<string> ReadFileContent(string filePath, string branchReference);

    Task CreateCheckRun(NewCheckRun checkRun);

    Task CreateComment(string commitId, string comment);

    Task<Issue> CreateIssue(NewIssue issue);
}