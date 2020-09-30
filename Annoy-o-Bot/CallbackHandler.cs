using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;

namespace Annoy_o_Bot
{
    public static class CallbackHandler
    {
        [FunctionName("Callback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB("annoydb", "reminders", ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<ReminderDocument> documents,
            [CosmosDB("annoydb", "reminders", ConnectionStringSetting = "CosmosDBConnection")]IDocumentClient documentClient,
            ILogger log)
        {
            if (!req.Headers.TryGetValue("X-GitHub-Event", out var callbackEvent) || callbackEvent != "push")
            {
                // this typically seem to be installation related events.
                log.LogWarning($"Non-push callback. 'X-GitHub-Event': '{callbackEvent}'");
                return new OkResult();
            }

            GitHubClient installationClient;
            CallbackModel requestObject;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(requestBody);
                requestObject = RequestParser.ParseJson(requestBody);
                installationClient = await GitHubHelper.GetInstallationClient(requestObject.Installation.Id);


            }
            catch (Exception e)
            {
                log.LogError(e, "Error at parsing callback input");
                throw;
            }

            var newReminders = await FindNewReminders(requestObject, installationClient);
            if (!requestObject.Ref.EndsWith($"/{requestObject.Repository.DefaultBranch}"))
            {
                await installationClient.Check.Run.Create(requestObject.Repository.Id,
                    new NewCheckRun("annoy-o-bot", requestObject.HeadCommit.Id)
                    {
                        Status = CheckStatus.Completed,
                        Conclusion = CheckConclusion.Success
                    });

                return new OkResult();
            }

            foreach ((string fileName, Reminder reminder) in newReminders)
            {
                await documents.AddAsync(new ReminderDocument
                {
                    Id = BuildDocumentId(requestObject, fileName),
                    InstallationId = requestObject.Installation.Id,
                    RepositoryId = requestObject.Repository.Id,
                    Reminder = reminder,
                    NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
                    Path = fileName
                });
                await CreateCommitComment(installationClient, requestObject,
                    $"Created reminder '{reminder.Title}' for {reminder.Date:D}");
            }

            await StoreNewReminders(documents, log, requestObject, installationClient);
            await DeleteRemovedReminders(documentClient, log, requestObject, installationClient);

            return new OkResult();
        }

        private static async Task<IList<(string, Reminder)>> FindNewReminders(CallbackModel requestObject,
            GitHubClient installationClient)
        {
            var reminderFiles = CommitParser.GetReminders(requestObject.Commits);
            var results = new List<(string, Reminder)>(reminderFiles.Length); // potentially lower but never higher than number of files
            foreach (string newFile in reminderFiles)
            {
                var reminderParser = GetReminderParser(newFile);
                if (reminderParser == null)
                {
                    // unsupported file type
                    continue;
                }

                var content =
                    await installationClient.Repository.Content.GetAllContents(requestObject.Repository.Id, newFile);
                var reminder = reminderParser.Parse(content.First().Content);
                results.Add((newFile, reminder));
            }

            return results;
        }

        private static async Task StoreNewReminders(IAsyncCollector<ReminderDocument> documents, ILogger log, CallbackModel requestObject,
            GitHubClient installationClient)
        {
            foreach (var newFile in CommitParser.GetReminders(requestObject.Commits))
            {
                try
                {
                    var reminderParser = GetReminderParser(newFile);
                    if (reminderParser == null)
                    {
                        // unsupported file type
                        continue;
                    }

                    var content =
                        await installationClient.Repository.Content.GetAllContents(requestObject.Repository.Id, newFile);
                    var reminder = reminderParser.Parse(content.First().Content);
                    await documents.AddAsync(new ReminderDocument
                    {
                        Id = BuildDocumentId(requestObject, newFile),
                        InstallationId = requestObject.Installation.Id,
                        RepositoryId = requestObject.Repository.Id,
                        Reminder = reminder,
                        NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
                        Path = newFile
                    });
                    await CreateCommitComment(installationClient, requestObject,
                        $"Created reminder '{reminder.Title}' for {reminder.Date:D}");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to create reminder");
                    await CreateCommitComment(installationClient, requestObject,
                        $"Failed to create reminder {newFile}: {string.Join(Environment.NewLine, e.Message, e.StackTrace)}");
                    throw;
                }
            }
        }

        static Task CreateCommitComment(GitHubClient client, CallbackModel request, string comment)
        {
            return client.Repository.Comment.Create(
                request.Repository.Id,
                request.HeadCommit.Id,
                new NewCommitComment(comment));
        }

        static string BuildDocumentId(CallbackModel request, string fileName)
        {
            return $"{request.Installation.Id}-{request.Repository.Id}-{fileName.Split('/').Last()}";
        }

        private static async Task DeleteRemovedReminders(IDocumentClient documentClient, ILogger log,
            CallbackModel requestObject, GitHubClient client)
        {
            var deletedReminders = CommitParser.GetDeletedReminders(requestObject.Commits);
            foreach (var deletedReminder in deletedReminders)
            {
                var reminderParser = GetReminderParser(deletedReminder);
                if (reminderParser == null)
                {
                    // unsupported file type
                    continue;
                }

                try
                {
                    var documentId = BuildDocumentId(requestObject, deletedReminder);
                    var documentUri = UriFactory.CreateDocumentUri("annoydb", "reminders", documentId);
                    await documentClient.DeleteDocumentAsync(documentUri,
                        new RequestOptions() {PartitionKey = new PartitionKey(documentId)});
                    await CreateCommitComment(client, requestObject, $"Deleted reminder '{deletedReminder}'");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to delete reminder");
                    await CreateCommitComment(
                        client, 
                        requestObject, 
                        $"Failed to delete reminder {deletedReminder}: {string.Join(Environment.NewLine, e.Message, e.StackTrace)}");
                    throw;
                }
            }
        }

        private static ReminderParser? GetReminderParser(string filePath)
        {
            if (filePath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                return JsonReminderParser.Value;
            }

            if (filePath.EndsWith(".yaml", StringComparison.InvariantCultureIgnoreCase))
            {
                return YamlReminderParser.Value;
            }

            return null;
        }

        static readonly Lazy<JsonReminderParser> JsonReminderParser = new Lazy<JsonReminderParser>(() => new JsonReminderParser());
        static readonly Lazy<YamlReminderParser> YamlReminderParser = new Lazy<YamlReminderParser>(() => new YamlReminderParser());
    }

    public class ReminderDocument
    {
        // assigning null using the null-forgiving operator because the value will always be set
        [JsonProperty("id")]
        public string Id { get; set; } = null!;
        public Reminder Reminder { get; set; } = null!;
        public long InstallationId { get; set; }
        public long RepositoryId { get; set; }
        public DateTime LastReminder { get; set; }
        public DateTime NextReminder { get; set; }
        public string Path { get; set; } = null!;
    }

    //TODO: support projects

}
