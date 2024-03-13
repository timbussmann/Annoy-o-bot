using Annoy_o_Bot.CosmosDB;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class CosmosFixture
{
    public Container CreateDocumentClient()
    {
        // CosmosDB emulator connection settings
        var cosmosClient = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        return cosmosClient.GetContainer(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId);
    }
}

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}