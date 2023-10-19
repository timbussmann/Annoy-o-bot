using Annoy_o_Bot.CosmosDB;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class CosmosFixture : IDisposable
{
    public CosmosFixture()
    {
        // CosmosDB emulator connection settings
        using var documentClient = CreateDocumentClient();

        documentClient.CreateDatabaseIfNotExistsAsync(new Database
        {
            Id = CosmosClientWrapper.dbName
        }).GetAwaiter().GetResult();
    }

    public DocumentClient CreateDocumentClient()
    {
        // CosmosDB emulator connection settings
        return new DocumentClient(
            new Uri("https://localhost:8081/"),
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
    }

    public void Dispose()
    {
    }
}

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}