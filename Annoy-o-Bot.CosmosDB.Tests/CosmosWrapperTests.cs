using System.Net;
using Annoy_o_Bot.AcceptanceTests;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Annoy_o_Bot.CosmosDB.Tests;

public class CosmosWrapperTests : IClassFixture<CosmosFixture>
{
    private CosmosFixture cosmosFixture;

    private Container DocumentClient;

    public CosmosWrapperTests(CosmosFixture cosmosFixture)
    {
        this.cosmosFixture = cosmosFixture;
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
            Path = "file/path.txt"
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
            Path = "file/path.txt"
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
            Path = "file/path.txt"
        };
        await wrapper.AddOrUpdateReminder(DocumentClient, existingReminder);

        var result = (await ExecuteReminderQuery()).Single();

        Assert.Equivalent(existingReminder, result);
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