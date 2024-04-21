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

public class DetectMissingReminders
{
    static readonly CosmosClientWrapper cosmsClientWrapper = new();
    readonly IGitHubApi githubApi;
    readonly ILogger<DetectMissingReminders> log;

    public DetectMissingReminders(IGitHubApi githubApi, ILogger<DetectMissingReminders> log)
    {
        this.githubApi = githubApi;
        this.log = log;
    }

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
        var documents = await cosmsClientWrapper.LoadAllReminders(cosmosContainer);
        log.LogInformation($"Loaded {documents.Count} reminders");

        var installations = documents.GroupBy(d => d.InstallationId);
        foreach (var byInstallation in installations)
        {
            var installationClient = await githubApi.GetInstallation(byInstallation.Key);

            foreach (var byRepository in byInstallation.GroupBy(i => i.RepositoryId))
            {
                var repository = await installationClient.GetRepository(byRepository.Key);
                var files = await repository.ReadAllRemindersFromDefaultBranch();
                foreach ((string path, string content) file in files)
                {
                    if (!byRepository.Any(reminder => reminder.Path == file.path))
                    {
                        log.LogError(
                            $"Missing reminder {file.path} in repository {byRepository.Key} (installation {byInstallation.Key})");

                        Reminder reminder;
                        try
                        {
                            reminder = LoadReminder(file.path, file.content);
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Unable to parse reminder {path}. Reminder definition: '{reminderContent}'", file.path, file.content);
                            continue;
                        }

                        await CreateReminder(file.path, reminder, byRepository.Key, byInstallation.Key, cosmosContainer, log);
                    }
                }

            }
        }
    }

    async Task CreateReminder(string filePath, Reminder reminder, long repositoryId, long installationId, Container cosmosContainer, ILogger log)
    {
        var reminderDocument = new ReminderDocument
        {
            InstallationId = installationId,
            RepositoryId = repositoryId,
            Reminder = reminder,
            NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
            Path = filePath
        };

        await cosmsClientWrapper.AddOrUpdateReminder(cosmosContainer, reminderDocument);
        log.LogInformation($"Created missing reminder for {reminderDocument.InstallationId}/{reminderDocument.RepositoryId}/{reminderDocument.Path}, due {reminderDocument.NextReminder}");
    }

    static Reminder LoadReminder(string filePath, string content)
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