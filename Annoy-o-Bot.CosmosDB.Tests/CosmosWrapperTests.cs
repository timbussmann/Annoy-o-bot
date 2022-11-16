using System.Collections.ObjectModel;
using System.Net;
using Annoy_o_Bot.AcceptanceTests;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace Annoy_o_Bot.CosmosDB.Tests;

//TODO split using partial classes
public class CosmosWrapperTests : IClassFixture<CosmosFixture>
{
    private CosmosFixture cosmosFixture;

    private DocumentClient DocumentClient;

    public CosmosWrapperTests(CosmosFixture cosmosFixture)
    {
        this.cosmosFixture = cosmosFixture;
        DocumentClient = cosmosFixture.CreateDocumentClient();
        try
        {
            DocumentClient.DeleteDocumentCollectionAsync(
                UriFactory.CreateDocumentCollectionUri(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId),
                new RequestOptions() { }).GetAwaiter().GetResult();
        }
        catch (DocumentClientException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound)
            {
                throw;
            }
        }

        DocumentClient.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(CosmosClientWrapper.dbName),
            new DocumentCollection()
            {
                Id = CosmosClientWrapper.collectionId,
                PartitionKey = new PartitionKeyDefinition() { Paths = new Collection<string>() { "/id" } }
            }).GetAwaiter().GetResult();

    }

    [Fact]
    public async Task LoadReminder_should_return_null_when_reminder_not_found()
    {
        var wrapper = new CosmosClientWrapper();
        var result = await wrapper.LoadReminder(DocumentClient, "somefilename.txt", 123, 456);

        Assert.Null(result);
    }

    [Fact]
    public void GetDueReminders_should_return_empty_collection_when_no_reminders()
    {
        var result = ExecuteReminderQuery();

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

        var result = ExecuteReminderQuery();

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

        var result = ExecuteReminderQuery();

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

        var result = ExecuteReminderQuery();

        Assert.Equivalent(existingReminder, result.Single());
    }

    private IQueryable<ReminderDocument> ExecuteReminderQuery()
    {
        var documentCollectionUri =
            UriFactory.CreateDocumentCollectionUri(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId);
        var result = DocumentClient.CreateDocumentQuery<ReminderDocument>(
            documentCollectionUri,
            CosmosClientWrapper.ReminderQuery,
            new FeedOptions { EnableCrossPartitionQuery = true });
        return result;
    }
}