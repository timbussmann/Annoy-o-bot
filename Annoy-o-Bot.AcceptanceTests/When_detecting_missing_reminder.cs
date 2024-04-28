using Annoy_o_Bot.AcceptanceTests.Fakes;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.CosmosDB.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class When_detecting_missing_reminder : AcceptanceTest
{
    [Fact]
    public async Task Should_create_missing_reminder_documents()
    {
        var gitHubApi = new FakeGitHubApi();
        var repository = gitHubApi.CreateNewRepository();
        var handler = new CallbackHandler(gitHubApi, configurationBuilder.Build(), NullLogger<CallbackHandler>.Instance);
        var testee = new DetectMissingReminders(gitHubApi, NullLogger<DetectMissingReminders>.Instance);

        var missingReminder = new ReminderDefinition()
        {
            Title = "A forgotten reminder",
            Date = DateTime.UtcNow.AddDays(-1),
            Interval = Interval.Weekly
        };
        repository.AddJsonReminder(".reminders/missingReminder.json", missingReminder);

        // Create existing reminder because there needs to be at least one known reminder for an installation so that we can identify the installation:
        var reminder = new ReminderDefinition
        {
            Title = "Some title for the reminder",
            Date = DateTime.UtcNow.AddDays(100),
            Interval = Interval.Weekly
        };
        var createCallback = repository.CommitNewReminder(reminder);
        var createRequest = CreateCallbackHttpRequest(createCallback);
        await handler.Run(createRequest, container);

        await testee.Run(new DefaultHttpContext().Request, container);

        var cosmosWrapper = new CosmosClientWrapper(container);
        var allReminders = await cosmosWrapper.LoadAllReminders();

        Assert.Equal(2, allReminders.Count);
        Assert.True(allReminders.Any(reminder => reminder.Path == ".reminders/missingReminder.json"));
        Assert.True(allReminders.Any(reminder => reminder.Path == createCallback.HeadCommit.Added[0]));
    }

    public When_detecting_missing_reminder(CosmosFixture cosmosFixture) : base(cosmosFixture)
    {
    }
}