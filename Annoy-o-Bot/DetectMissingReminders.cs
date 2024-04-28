using System;
using System.Linq;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Annoy_o_Bot.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Annoy_o_Bot;

public class DetectMissingReminders(IGitHubApi githubApi, ILogger<DetectMissingReminders> log)
{
    [Function("DetectMissingReminders")]
    public async Task Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequest req,
        [CosmosDBInput(
            databaseName: CosmosClientWrapper.dbName,
            containerName: CosmosClientWrapper.collectionId,
            Connection = "CosmosDBConnection")]
        Container cosmosContainer)
    {
        CosmosClientWrapper cosmosClientWrapper = new(cosmosContainer);

        var documents = await cosmosClientWrapper.LoadAllReminders();
        log.LogInformation($"Loaded {documents.Count} reminders");

        var installations = documents.GroupBy(d => d.InstallationId);
        foreach (var byInstallation in installations)
        {
            var installationClient = await githubApi.GetInstallation(byInstallation.Key);

            foreach (var byRepository in byInstallation.GroupBy(i => i.RepositoryId))
            {
                var repository = await installationClient.GetRepository(byRepository.Key);
                var exisingReminderFilePaths = byRepository.Select(r => r.Path).ToHashSet();
                var reminderPaths = await repository.ReadAllRemindersFromDefaultBranch();

                // Check for reminder documents with no definition
                foreach (var existingReminder in exisingReminderFilePaths)
                {
                    if (!reminderPaths.Contains(existingReminder))
                    {
                        log.LogWarning("Couldn't find reminder definition on GitHub for reminder with path '{path}' in repository '{repoId}'", existingReminder, byRepository.Key);
                    }
                }

                // Check for reminder definitions with no reminder document
                foreach (var filePath in reminderPaths)
                {
                    if (!exisingReminderFilePaths.Contains(filePath))
                    {
                        log.LogError($"Missing reminder {filePath} in repository {byRepository.Key} (installation {byInstallation.Key})");

                        var reminderDefinitionText = string.Empty;
                        ReminderDefinition reminderDefinition;
                        try
                        {
                            reminderDefinitionText = await repository.ReadFileContent(filePath);
                            reminderDefinition = LoadReminder(filePath, reminderDefinitionText);
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Unable to parse reminder {path}. Reminder definition: '{reminderContent}'", filePath, reminderDefinitionText);
                            continue;
                        }

                        await CreateReminder(filePath, reminderDefinition, byRepository.Key, byInstallation.Key, cosmosClientWrapper, log);
                    }
                }

            }
        }
    }

    async Task CreateReminder(string filePath, ReminderDefinition reminderDefinition, long repositoryId, long installationId, ICosmosClientWrapper cosmosContainer, ILogger log)
    {
        var reminderDocument = new ReminderDocument
        {
            InstallationId = installationId,
            RepositoryId = repositoryId,
            Reminder = reminderDefinition,
            NextReminder = new DateTime(reminderDefinition.Date.Ticks, DateTimeKind.Utc),
            Path = filePath
        };

        await cosmosContainer.AddOrUpdateReminder(reminderDocument);
        log.LogInformation($"Created missing reminder for {reminderDocument.InstallationId}/{reminderDocument.RepositoryId}/{reminderDocument.Path}, due {reminderDocument.NextReminder}");
    }

    static ReminderDefinition LoadReminder(string filePath, string content)
    {
        var parser = ReminderParser.GetParser(filePath);
        if (parser == null)
        {
            // unsupported file type
            throw new NotSupportedException($"no parser available for {filePath}");
        }

        return parser.Parse(content);
    }
}