using Annoy_o_Bot.GitHub.Api;
using Xunit;

namespace Annoy_o_Bot.Tests.GitHub;

public class IssueMappingTests
{
    [Fact]
    public void ToGitHubIssue_ShouldMapReminderDefinitionToGitHubIssue()
    {
        var reminderDefinition = new ReminderDefinition
        {
            Title = "reminder title",
            Message = "reminder body",
            Assignee = "user1;user2;",
            Labels = ["label1", "label2", "label3"],
            Date = default,
            Interval = Interval.Once
        };

        var gitHubIssue = reminderDefinition.ToGitHubIssue();

        Assert.Equal(reminderDefinition.Title, gitHubIssue.Title);
        Assert.Equal(reminderDefinition.Message, gitHubIssue.Body);
        Assert.Equal(["user1", "user2"], gitHubIssue.Assignees);
        Assert.Equal(reminderDefinition.Labels, gitHubIssue.Labels);
        Assert.Null(gitHubIssue.Milestone);
    }
}