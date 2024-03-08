using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Annoy_o_Bot;

public class DetectMissingReminders
{
    readonly IGitHubApi gitHubApi;
    readonly CosmosClientWrapper cosmosWrapper;

    public DetectMissingReminders(IGitHubApi gitHubApi)
    {
        this.gitHubApi = gitHubApi;
        this.cosmosWrapper = new CosmosClientWrapper();
    }

    [Function("TimeoutFunction")]
    public async Task Run(
        //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        [TimerTrigger("0 */10 * * * *", RunOnStartup = true)]
        TimerInfo timer, // once every 10 minutes
        [CosmosDBInput(
            databaseName: CosmosClientWrapper.dbName,
            containerName: CosmosClientWrapper.collectionId,
            Connection = "CosmosDBConnection")]
        Container documentClient,
        ILogger log)
    {
        List<ReminderDocument> documents = new List<ReminderDocument>();
        var queryIterator = documentClient.GetItemQueryIterator<ReminderDocument>();
        while (queryIterator.HasMoreResults)
        {
            var result = await queryIterator.ReadNextAsync();
            documents.AddRange(documents);
        }

        var installations = documents.GroupBy(d => d.InstallationId);
        foreach (var byInstallation in installations)
        {
            var installationClient = await GitHubHelper.GetInstallationClient(byInstallation.Key);

            foreach (var byRepository in byInstallation.GroupBy(i => i.RepositoryId))
            {
                var files = await installationClient.Repository.Content.GetAllContents(byRepository.Key, ".reminders");
                foreach (var file in files)
                {
                    if (!byRepository.Any(reminder => reminder.Path == file.Path))
                    {
                        log.LogError($"Missing reminder {file.Path}");
                    }
                }

            }
        }
    }
}