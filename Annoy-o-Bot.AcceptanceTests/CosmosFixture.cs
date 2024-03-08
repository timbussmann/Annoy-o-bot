using Annoy_o_Bot.CosmosDB;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class CosmosFixture : IDisposable
{
    public CosmosFixture()
    {
        // CosmosDB emulator connection settings
        var client = CreateDocumentClient();

        client.CreateDatabaseIfNotExistsAsync(CosmosClientWrapper.dbName).GetAwaiter().GetResult();
    }

    public CosmosClient CreateDocumentClient()
    {
        // CosmosDB emulator connection settings
        var cosmosClient = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        return cosmosClient;
    }

    public void Dispose()
    {
    }
}

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}