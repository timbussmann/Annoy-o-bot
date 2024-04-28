using System.Net;
using Annoy_o_Bot.AcceptanceTests;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Annoy_o_Bot.CosmosDB.Tests;

public class CosmosWrapperTests : IClassFixture<CosmosFixture>
{
    Container DocumentClient;

    public CosmosWrapperTests(CosmosFixture cosmosFixture)
    {
        DocumentClient = cosmosFixture.CreateDocumentClient();
        try
        {
            DocumentClient.DeleteContainerAsync().GetAwaiter().GetResult();
        }
        catch (CosmosException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound)
            {
                throw;
            }
        }

        DocumentClient.Database.CreateContainerAsync(new ContainerProperties(CosmosClientWrapper.collectionId, "/id")).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task LoadReminder_should_return_null_when_reminder_not_found()
    {
        var wrapper = new CosmosClientWrapper();
        var result = await wrapper.LoadReminder(DocumentClient, "somefilename.txt", 123, 456);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDueReminders_should_return_empty_collection_when_no_reminders()
    {
        var result = await ExecuteReminderQuery();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueReminder_should_return_empty_collection_when_no_reminder_is_due()
    {
        var wrapper = new CosmosClientWrapper();
        await wrapper.AddOrUpdateReminder(DocumentClient, new ReminderDocument
        {
            NextReminder = DateTime.UtcNow.AddMinutes(1),
            InstallationId = Random.Shared.NextInt64(),
            RepositoryId = Random.Shared.NextInt64(),
            Path = "file/path.txt",
            Reminder = new ReminderDefinition()
        });

        var result = await ExecuteReminderQuery();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueReminder_should_return_empty_collection_when_no_reminder_has_due_date()
    {
        var wrapper = new CosmosClientWrapper();
        await wrapper.AddOrUpdateReminder(DocumentClient, new ReminderDocument
        {
            NextReminder = null,
            InstallationId = Random.Shared.NextInt64(),
            RepositoryId = Random.Shared.NextInt64(),
            Path = "file/path.txt",
            Reminder = new ReminderDefinition()
        });

        var result = await ExecuteReminderQuery();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueReminder_should_return_due_reminders()
    {
        var wrapper = new CosmosClientWrapper();
        var existingReminder = new ReminderDocument
        {
            NextReminder = DateTime.UtcNow.AddMinutes(-1),
            InstallationId = Random.Shared.NextInt64(),
            RepositoryId = Random.Shared.NextInt64(),
            Path = "file/path.txt",
            Reminder = new ReminderDefinition()
        };
        await wrapper.AddOrUpdateReminder(DocumentClient, existingReminder);

        var result = (await ExecuteReminderQuery()).Single();

        Assert.Equivalent(existingReminder, result);
    }

    [Fact]
    public async Task AddOrUpdateReminder_should_create_missing_reminder()
    {
        var wrapper = new CosmosClientWrapper();
        var existingReminder = new ReminderDocument
        {
            LastReminder = new DateTime(2010, 10, 10),
            NextReminder = new DateTime(2012, 12, 12),
            InstallationId = Random.Shared.NextInt64(),
            RepositoryId = Random.Shared.NextInt64(),
            Path = "file/path.txt",
            Reminder = new ReminderDefinition()
            {
                Assignee = "demo assignee",
                Date = DateTime.MinValue,
                Interval = Interval.Monthly,
                IntervalStep = 4,
                Labels = new[] {"label1", "label2"},
                Title = "demo title"
            }
        };

        await wrapper.AddOrUpdateReminder(DocumentClient, existingReminder);
        var storedReminder = await wrapper.LoadReminder(DocumentClient, existingReminder.Path, existingReminder.InstallationId,
            existingReminder.RepositoryId);

        Assert.Equivalent(existingReminder, storedReminder);
    }

    [Fact]
    public async Task AddOrUpdateReminder_should_update_existing_reminder()
    {
        var wrapper = new CosmosClientWrapper();
        var existingReminder = new ReminderDocument
        {
            LastReminder = new DateTime(2010, 10, 10),
            NextReminder = new DateTime(2012, 12, 12),
            InstallationId = Random.Shared.NextInt64(),
            RepositoryId = Random.Shared.NextInt64(),
            Path = "file/path.txt",
            Reminder = new ReminderDefinition()
            {
                Assignee = "demo assignee",
                Date = DateTime.MinValue,
                Interval = Interval.Monthly,
                IntervalStep = 4,
                Labels = new[] { "label1", "label2" },
                Title = "demo title"
            }
        };

        await wrapper.AddOrUpdateReminder(DocumentClient, existingReminder);

        existingReminder.Reminder.Title = "updated title";
        await wrapper.AddOrUpdateReminder(DocumentClient, existingReminder);

        var updatedReminder = await wrapper.LoadReminder(DocumentClient, existingReminder.Path, existingReminder.InstallationId,
            existingReminder.RepositoryId);

        Assert.Equivalent(existingReminder, updatedReminder);
    }

    async Task<List<ReminderDocument>> ExecuteReminderQuery()
    {
        var queryIterator = DocumentClient.GetItemQueryIterator<ReminderDocument>(CosmosClientWrapper.ReminderQuery);
        List<ReminderDocument> reminders = new();
        while (queryIterator.HasMoreResults)
        {
            var readResult = await queryIterator.ReadNextAsync();
            reminders.AddRange(readResult.Resource);
        }

        return reminders;
    }
}