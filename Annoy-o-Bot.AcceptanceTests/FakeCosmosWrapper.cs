using Annoy_o_Bot.CosmosDB;
using Microsoft.Azure.Documents;
using Xunit;

namespace Annoy_o_Bot.AcceptanceTests;

public class FakeCosmosWrapper : ICosmosClientWrapper
{
    public long expectedInstallationId { get; set; }
    public long expectedRepositoryId { get; set; }

    public FakeCosmosWrapper(long expectedInstallationId, long expectedRepositoryId)
    {
        this.expectedInstallationId = expectedInstallationId;
        this.expectedRepositoryId = expectedRepositoryId;
    }

    public Dictionary<string, ReminderDocument> StoredReminders { get; set; } =
        new Dictionary<string, ReminderDocument>();
    public Task<ReminderDocument> LoadReminder(IDocumentClient cosmosClient, string fileName, long installationId, long repositoryId)
    {
        Assert.Equal(expectedInstallationId, installationId);
        Assert.Equal(expectedRepositoryId, repositoryId);

        //TODO contract test to verify behavior on existing/non-existing file
        return Task.FromResult((ReminderDocument)StoredReminders[fileName].Clone());
    }
}