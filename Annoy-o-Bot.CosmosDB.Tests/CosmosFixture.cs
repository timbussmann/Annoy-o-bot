using Annoy_o_Bot.CosmosDB;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class CosmosFixture : IDisposable
{
    public DocumentClient DocumentClient { get; private set; }

    public CosmosFixture()
    {
        // CosmosDB emulator connection settings
        DocumentClient = new DocumentClient(
            new Uri("https://localhost:8081/"),
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
    }

    public void Dispose()
    {
#if RELEASE
        var collectionUri =
            UriFactory.CreateDocumentCollectionUri(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId);
        DocumentClient.DeleteDocumentCollectionAsync(collectionUri).GetAwaiter().GetResult();
#endif
    }
}

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}