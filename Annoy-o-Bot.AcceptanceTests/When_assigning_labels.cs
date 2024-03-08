using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_assigning_labels : AcceptanceTest
{
    [Fact]
    public async Task Should_assign_labels_to_issue()
    {
        var gitHubApi = new FakeGitHubApi();
        var repository = gitHubApi.CreateNewRepository();
        var reminder = new Reminder
        {
            Title = "Some title for the new reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly,
            Labels = new[] { "label1", "label2" }
        };
        var callback = repository.CommitNewReminder(reminder);
        var request = CreateCallbackHttpRequest(callback);

        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var result = await handler.Run(request, container, NullLogger.Instance);

        Assert.IsType<OkResult>(result);
        await CreateDueReminders(gitHubApi);

        var issue = Assert.Single(repository.Issues);
        Assert.Contains("label1", issue.Labels);
        Assert.Contains("label2", issue.Labels);
        Assert.Equal(2, issue.Labels.Count);
    }

    public When_assigning_labels(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}