using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

[CollectionDefinition("CosmosDB")]
public class CosmosTests : ICollectionFixture<CosmosFixture>
{
}