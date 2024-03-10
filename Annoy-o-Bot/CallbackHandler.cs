using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annoy_o_Bot.CosmosDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using Annoy_o_Bot.Parser;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Worker;

namespace Annoy_o_Bot
{
    public class CallbackHandler
    {
        readonly IGitHubApi gitHubApi;
        readonly IConfiguration configuration;
        readonly ICosmosClientWrapper cosmosWrapper;
        readonly ILogger<CallbackHandler> log;

        public CallbackHandler(IGitHubApi gitHubApi, IConfiguration configuration, ILogger<CallbackHandler> log)
        {
            this.gitHubApi = gitHubApi;
            this.configuration = configuration;
            this.log = log;
            this.cosmosWrapper = new CosmosClientWrapper();
        }

        [Function("Callback")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            [CosmosDBInput(
                databaseName: CosmosClientWrapper.dbName,
                containerName: CosmosClientWrapper.collectionId,
                Connection = "CosmosDBConnection")]
            Container cosmosContainer)
        {
            GitHubHelper.ValidateRequest(req, configuration.GetValue<string>("WebhookSecret") ?? throw new Exception("Missing 'WebhookSecret' env var"), log);
            if (!req.Headers.TryGetValue("X-GitHub-Event", out var callbackEvent) || callbackEvent != "push")
            {
                if (callbackEvent != "check_suite") // ignore check_suite events
                {
                    // this typically seem to be installation related events.
                    log.LogWarning($"Non-push callback. 'X-GitHub-Event': '{callbackEvent}'");
                }

                return new OkResult();
            }

            var requestObject = await ParseRequest(req, log);
            var githubClient = await gitHubApi.GetRepository(requestObject.Installation.Id, requestObject.Repository.Id);

            if (requestObject.HeadCommit == null)
            {
                // no commits on push (e.g. branch delete)
                return new OkResult();
            }

            log.LogInformation($"Handling changes made to branch '{requestObject.Repository.Name}{requestObject.Ref}' by head-commit '{requestObject.HeadCommit.Id}'.");

            var commitsToConsider = requestObject.Commits;
            if (commitsToConsider.LastOrDefault()?.Message?.StartsWith("Merge ") ?? false)
            {
                // if the last commit is a merge commit, ignore other commits as the merge commits contains all the relevant changes
                // TODO: This behavior will be incorrect if a non-merge-commit contains this commit message. To be absolutely sure, we'd have to retrieve the full commit object and inspect the parent information. This information is not available on the callback object
                commitsToConsider = new[] { commitsToConsider.Last() };
            }

            var fileChanges = CommitParser.GetChanges(commitsToConsider);
            var reminderChanges = ReminderFilter.FilterReminders(fileChanges);

            if (requestObject.Ref.EndsWith($"/{requestObject.Repository.DefaultBranch}"))
            {
                var newReminders = await LoadReminder(reminderChanges.New, requestObject, githubClient);
                foreach (var (fileName, reminder) in newReminders)
                {
                    var reminderDocument = new ReminderDocument
                    {
                        InstallationId = requestObject.Installation.Id,
                        RepositoryId = requestObject.Repository.Id,
                        Reminder = reminder,
                        NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
                        Path = fileName
                    };

                    await cosmosWrapper.AddOrUpdateReminder(cosmosContainer, reminderDocument);
                    await githubClient.CreateComment(requestObject.HeadCommit.Id,
                        $"Created reminder '{reminder.Title}' for {reminder.Date:D}");
                }

                var updatedReminders = await LoadReminder(reminderChanges.Updated, requestObject, githubClient);
                foreach (var (fileName, updatedReminder) in updatedReminders)
                {
                    var existingReminder = await cosmosWrapper.LoadReminder(cosmosContainer, fileName, requestObject.Installation.Id, requestObject.Repository.Id);

                    existingReminder!.Reminder = updatedReminder;
                    // recalculate next reminder due time from scratch:
                    existingReminder.NextReminder = new DateTime(updatedReminder.Date.Ticks, DateTimeKind.Utc);
                    if (existingReminder.LastReminder >= existingReminder.NextReminder)
                    {
                        // reminder start date is in the past, re-calculate next reminder due date with interval based on new start date.
                        existingReminder.CalculateNextReminder(DateTime.Now);
                    }

                    await cosmosWrapper.AddOrUpdateReminder(cosmosContainer, existingReminder);
                    await githubClient.CreateComment(requestObject.HeadCommit.Id,
                        $"Updated reminder '{updatedReminder.Title}' for {existingReminder.NextReminder:D}");
                }

                await DeleteRemovedReminders(fileChanges.Deleted, cosmosContainer, log, requestObject, githubClient);
            }
            else
            {
                List<(string, Reminder)> newReminders;
                try
                {
                    newReminders = await LoadReminder(reminderChanges.New, requestObject, githubClient);
                    newReminders.AddRange(await LoadReminder(reminderChanges.Updated, requestObject, githubClient));
                }
                catch (Exception e)
                {
                    await TryCreateCheckRun(githubClient, requestObject.Repository.Id,
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
                    await TryCreateCheckRun(githubClient, requestObject.Repository.Id,
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
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                requestObject = RequestParser.ParseJson(requestBody);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error at parsing callback input");
                throw;
            }

            return requestObject;
        }

        private static async Task TryCreateCheckRun(IGitHubRepository installationClient, long repositoryId, NewCheckRun checkRun, ILogger logger)
        {
            // Ignore check run failures for now. Check run permissions were added later, so users might not have granted permissions to add check runs.
            try
            {
                await installationClient.CreateCheckRun(checkRun);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Failed to create check run for repository {repositoryId}.");
            }
        }

        static async Task<List<(string, Reminder)>> LoadReminder(ICollection<string> filePaths, CallbackModel requestObject, IGitHubRepository installationClient)
        {
            var results = new List<(string, Reminder)>(filePaths.Count); // potentially lower but never higher than number of files
            foreach (var filePath in filePaths)
            {
                var parser = ReminderParser.GetParser(filePath);
                if (parser == null)
                {
                    // unsupported file type
                    continue;
                }

                var content = await installationClient.ReadFileContent(filePath, requestObject.Ref);
                var reminder = parser.Parse(content);
                results.Add((filePath, reminder));
            }

            return results;
        }

        async Task DeleteRemovedReminders(ICollection<string> deletedFiles, Container documentClient, ILogger log, CallbackModel requestObject, IGitHubRepository client)
        {
            foreach (var deletedReminder in deletedFiles)
            {
                var reminderParser = ReminderParser.GetParser(deletedReminder);
                if (reminderParser == null)
                {
                    // unsupported file type
                    continue;
                }

                try
                {
                    await cosmosWrapper.Delete(documentClient, deletedReminder, requestObject.Installation.Id,
                        requestObject.Repository.Id);
                    await client.CreateComment(requestObject.HeadCommit.Id,
                        $"Deleted reminder '{deletedReminder}'");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to delete reminder");
                    await client.CreateComment(requestObject.HeadCommit.Id,
                        $"Failed to delete reminder {deletedReminder}: {string.Join(Environment.NewLine, e.Message, e.StackTrace)}");
                }
            }
        }
    }
}
