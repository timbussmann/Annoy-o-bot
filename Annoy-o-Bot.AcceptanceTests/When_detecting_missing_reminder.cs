using Annoy_o_Bot.AcceptanceTests.Fakes;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_detecting_missing_reminder : AcceptanceTest
{
    [Fact]
    public async Task Should_create_missing_reminders()
    {
        var gitHubApi = new FakeGitHubApi();
        var repository = gitHubApi.CreateNewRepository();
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build());
        var testee = new DetectMissingReminders(gitHubApi);

        var missingReminder = new Reminder()
        {
            Title = "A forgotten reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        repository.AddJsonReminder(".reminders/missingReminder.json", missingReminder);

        // Create existing reminder:
        var reminder = new Reminder
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(100),
            Interval = Interval.Weekly
        };
        var createCallback = repository.CommitNewReminder(reminder);
        var createRequest = CreateCallbackHttpRequest(createCallback);
        await handler.Run(createRequest, documentClient, NullLogger.Instance);

        await testee.Run(new DefaultHttpRequest(new DefaultHttpContext()), documentClient, NullLogger.Instance);

        await CreateDueReminders(gitHubApi);

         var issue = Assert.Single(repository.Issues);
        Assert.Equal(missingReminder.Title, issue.Title);
    }

    public When_detecting_missing_reminder(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}