using Annoy_o_Bot.CosmosDB;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class CosmosFixture
{
    const string EmulatorConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public Container CreateDocumentClient()
    {
        /*
         * The Azure Function worker uses the JsonObjectSerializer for serialization instead of the Newtonsoft default serializer
         * normally used by the Cosmos SDK. This causes a mismatch when relying on attributes to control serialization (e.g. property names).
         * This uses a configuration as close as possible to the Azure Functions runtime configuration to make sure the tests use the same serialization
         * behavior as in function environment.
         */
        var options = new CosmosClientOptions()
        {
            Serializer = new WorkerCosmosSerializer()
        };
        // CosmosDB emulator connection settings
        var cosmosConnectinString = Environment.GetEnvironmentVariable("CosmosDBConnectionString") ?? EmulatorConnectionString;
        var cosmosClient = new CosmosClient(cosmosConnectinString, options);
        return cosmosClient.GetContainer(CosmosClientWrapper.dbName, CosmosClientWrapper.collectionId);
    }
}

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}

/// <summary>
/// See https://github.com/Azure/azure-functions-dotnet-worker/blob/main/extensions/Worker.Extensions.CosmosDB/src/WorkerCosmosSerializer.cs
/// </summary>
class WorkerCosmosSerializer : CosmosSerializer
{
    readonly JsonObjectSerializer jsonSerializer = JsonObjectSerializer.Default;

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            return (T)jsonSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var memoryStream = new MemoryStream();
        jsonSerializer.Serialize(memoryStream, input, typeof(T), default);
        memoryStream.Position = 0;
        return memoryStream;
    }
}