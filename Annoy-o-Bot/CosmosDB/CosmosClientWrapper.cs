using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Annoy_o_Bot.CosmosDB;

public class CosmosClientWrapper : ICosmosClientWrapper
{
    internal const string dbName = "annoydb";
    internal const string collectionId = "reminders";

    public async Task<ReminderDocument?> LoadReminder(IDocumentClient cosmosClient, string fileName, long installationId,
        long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);
        var documentUri = UriFactory.CreateDocumentUri(dbName, collectionId, documentId);

        var existingReminder = await cosmosClient.ReadDocumentAsync<ReminderDocument>(
            documentUri,
            new RequestOptions { PartitionKey = new PartitionKey(documentId) });
        return existingReminder.Document;
    }

    public async Task Delete(IDocumentClient cosmosClient, string fileName, long installationId, long repositoryId)
    {
        var documentId = BuildDocumentId(fileName, installationId, repositoryId);
        var documentUri = UriFactory.CreateDocumentUri(dbName, collectionId, documentId);
        await cosmosClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(documentId) });
    }

    static string BuildDocumentId(string fileName, long installationId, long repositoryId)
    {
        return $"{installationId}-{repositoryId}-{fileName.Split('/').Last()}";
    }
}