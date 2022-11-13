using Annoy_o_Bot.AcceptanceTests;
using Xunit;

namespace Annoy_o_Bot.CosmosDB.Tests;

public class CosmosWrapperTests : IClassFixture<CosmosFixture>
{
    private CosmosFixture cosmosFixture;

    public CosmosWrapperTests(CosmosFixture cosmosFixture)
    {
        this.cosmosFixture = cosmosFixture;
    }

    [Fact]
    public async Task LoadReminder_should_return_null_when_reminder_not_found()
    {
        var wrapper = new CosmosClientWrapper();
        var result = await wrapper.LoadReminder(cosmosFixture.DocumentClient, "somefilename.txt", 123, 456);

        Assert.Null(result);
    }
}