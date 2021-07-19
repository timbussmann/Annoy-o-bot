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
using Octokit;

namespace Annoy_o_Bot
{
    public static class CallbackHandler
    {
        const string dbName = "annoydb";
        const string collectionId = "reminders";

        [FunctionName("Callback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(dbName, collectionId, ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<ReminderDocument> documents,
            [CosmosDB(dbName, collectionId, ConnectionStringSetting = "CosmosDBConnection")]IDocumentClient documentClient,
            ILogger log)
        {
            if (!req.Headers.TryGetValue("X-GitHub-Event", out var callbackEvent) || callbackEvent != "push")
            {
                // this typically seem to be installation related events.
                // Or check_run (action:requested/rerequested) / check_suite events.
                log.LogWarning($"Non-push callback. 'X-GitHub-Event': '{callbackEvent}'");
                return new OkResult();
            }

            var requestObject = await ParseRequest(req, log);
            var installationClient = await GitHubHelper.GetInstallationClient(requestObject.Installation.Id);

            if (requestObject.HeadCommit == null)
            {
                // no commits on push (e.g. branch delete)
                return new OkResult();
            }

            log.LogInformation($"Handling changes made to branch '{requestObject.Ref}' by head-commit '{requestObject.HeadCommit}'.");

            if (requestObject.Ref.EndsWith($"/{requestObject.Repository.DefaultBranch}"))
            {
                var commitsToConsider = requestObject.Commits;
                if (commitsToConsider.LastOrDefault()?.Message?.StartsWith("Merge ") ?? false)
                {
                    // if the last commit is a merge commit, ignore other commits as the merge commits contains all the relevant changes
                    // TODO: This behavior will be incorrect if a non-merge-commit contains this commit message. To be absolutely sure, we'd have to retrieve the full commit object and inspect the parent information. This information is not available on the callback object
                    commitsToConsider = new[] {commitsToConsider.Last()};
                }

                var fileChanges = CommitParser.GetChanges(commitsToConsider);
                var reminderChanges = ReminderFilter.FilterReminders(fileChanges);
                var newReminders = await LoadReminder(reminderChanges.New, requestObject, installationClient);
                foreach ((string fileName, Reminder reminder) in newReminders)
                {
                    var reminderDocument = new ReminderDocument
                    {
                        Id = BuildDocumentId(requestObject, fileName),
                        InstallationId = requestObject.Installation.Id,
                        RepositoryId = requestObject.Repository.Id,
                        Reminder = reminder,
                        NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
                        Path = fileName
                    };

                    await documents.AddAsync(reminderDocument);
                    await CreateCommitComment(installationClient, requestObject,
                        $"Created reminder '{reminder.Title}' for {reminder.Date:D}");
                }

                var updatedReminders = await LoadReminder(reminderChanges.Updated, requestObject, installationClient);
                foreach ((string fileName, Reminder reminder) in updatedReminders)
                {
                    var documentId = BuildDocumentId(requestObject, fileName);
                    UriFactory.CreateDocumentUri(dbName, collectionId, documentId);
                    var existingReminder = await documentClient.ReadDocumentAsync<ReminderDocument>(documentId); //TODO do we need a sharding key?

                    existingReminder.Document.Reminder = reminder;
                    // recalculate next reminder due time from scratch:
                    existingReminder.Document.NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc);
                    existingReminder.Document.CalculateNextReminder(DateTime.Now);

                    await documents.AddAsync(existingReminder.Document);
                    await CreateCommitComment(installationClient, requestObject,
                        $"Updated reminder '{reminder.Title}' for {existingReminder.Document.NextReminder:D}");
                }

                await DeleteRemovedReminders(fileChanges.Deleted, documentClient, log, requestObject, installationClient);
            }
            else
            {
                IList<(string, Reminder)> newReminders;
                try
                {
                    // inspect all commits on branches as we just want to see whether they are valid
                    newReminders = await FindNewReminders(requestObject.Commits, requestObject, installationClient);
                }
                catch (Exception e)
                {
                    await TryCreateCheckRun(installationClient, requestObject.Repository.Id,
                        new NewCheckRun("annoy-o-bot", requestObject.HeadCommit.Id)
                        {
                            Status = CheckStatus.Completed,
                            Conclusion = CheckConclusion.Failure,
                            Output = new NewCheckRunOutput(
                                "Invalid reminder definition",
                                "The provided reminder seems to be invalid or incorrect." + e.Message)
                        }, log);
                    throw;
                }

                if (newReminders.Any())
                {
                    await TryCreateCheckRun(installationClient, requestObject.Repository.Id,
                        new NewCheckRun("annoy-o-bot", requestObject.HeadCommit.Id)
                        {
                            Status = CheckStatus.Completed,
                            Conclusion = CheckConclusion.Success
                        }, log);
                }
            }

            return new OkResult();
        }

        private static async Task<CallbackModel> ParseRequest(HttpRequest req, ILogger log)
        {
            CallbackModel requestObject;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                requestObject = RequestParser.ParseJson(requestBody);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error at parsing callback input");
                throw;
            }

            return requestObject;
        }

        private static async Task TryCreateCheckRun(GitHubClient installationClient, long repositoryId, NewCheckRun checkRun, ILogger logger)
        {
            // Ignore check run failures for now. Check run permissions were added later, so users might not have granted permissions to add check runs.
            try
            {
                await installationClient.Check.Run.Create(repositoryId, checkRun);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Failed to create check run for repository {repositoryId}.");
            }
        }

        private static async Task<IList<(string, Reminder)>> FindNewReminders(
            CallbackModel.CommitModel[] commits, CallbackModel requestObject,
            GitHubClient installationClient)
        {
            var reminderFiles = CommitParser.GetReminders(commits);
            var results = new List<(string, Reminder)>(reminderFiles.Length); // potentially lower but never higher than number of files
            foreach (string newFile in reminderFiles)
            {
                var reminderParser = GetReminderParser(newFile);
                if (reminderParser == null)
                {
                    // unsupported file type
                    continue;
                }

                var content = await installationClient.Repository.Content.GetAllContentsByRef(
                    requestObject.Repository.Id, 
                    newFile, 
                    requestObject.Ref);
                var reminder = reminderParser.Parse(content.First().Content);
                results.Add((newFile, reminder));
            }

            return results;
        }

        static async Task<IList<(string, Reminder)>> LoadReminder(ICollection<string> filePaths, CallbackModel requestObject, GitHubClient installationClient)
        {
            var results = new List<(string, Reminder)>(filePaths.Count); // potentially lower but never higher than number of files
            foreach (var filePath in filePaths)
            {
                var parser = GetReminderParser(filePath);
                if (parser == null)
                {
                    // unsupported file type
                    continue;
                }

                var content = await installationClient.Repository.Content.GetAllContentsByRef(
                    requestObject.Repository.Id,
                    filePath,
                    requestObject.Ref);
                var reminder = parser.Parse(content.First().Content);
                results.Add((filePath, reminder));
            }

            return results;
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

        static async Task DeleteRemovedReminders(ICollection<string> deletedFiles, IDocumentClient documentClient, ILogger log, CallbackModel requestObject, GitHubClient client)
        {
            foreach (var deletedReminder in deletedFiles)
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
                        new RequestOptions {PartitionKey = new PartitionKey(documentId)});
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

        static ReminderParser? GetReminderParser(string filePath)
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
}
