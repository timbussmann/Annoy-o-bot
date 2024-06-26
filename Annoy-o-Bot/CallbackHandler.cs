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
using Microsoft.Extensions.Primitives;

namespace Annoy_o_Bot
{
    public class CallbackHandler(IGitHubApi gitHubApi, IConfiguration configuration, ILogger<CallbackHandler> log)
    {
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
            var cosmosWrapper = new CosmosClientWrapper(cosmosContainer);
            await GitHubHelper.ValidateRequest(req, configuration.GetValue<string>("WebhookSecret") ?? throw new Exception("Missing 'WebhookSecret' env var"), log);

            if (!IsGitCommitCallback(req))
            {
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
                commitsToConsider = [commitsToConsider.Last()];
            }

            var fileChanges = CommitParser.GetChanges(commitsToConsider);
            var reminderChanges = ReminderFilter.FilterReminders(fileChanges);

            if (requestObject.Ref.EndsWith($"/{requestObject.Repository.DefaultBranch}"))
            {
                await ApplyReminderDefinitions(reminderChanges, requestObject, githubClient, cosmosWrapper, fileChanges);
            }
            else
            {
                await ValidateReminderDefinitions(reminderChanges, requestObject, githubClient);
            }

            return new OkResult();
        }

        async Task ApplyReminderDefinitions(FileChanges reminderChanges, CallbackModel requestObject,
            IGitHubRepository githubClient, CosmosClientWrapper cosmosWrapper, FileChanges fileChanges)
        {
            var newReminders = await LoadReminders(reminderChanges.New, requestObject, githubClient);
            foreach (var (fileName, reminder) in newReminders)
            {
                await CreateNewReminder(cosmosWrapper, requestObject, reminder, fileName, githubClient);
            }

            var updatedReminders = await LoadReminders(reminderChanges.Updated, requestObject, githubClient);
            foreach (var (fileName, updatedReminder) in updatedReminders)
            {
                var existingReminder = await cosmosWrapper.LoadReminder(fileName, requestObject.Installation.Id, requestObject.Repository.Id);
                if (existingReminder is null)
                {
                    await CreateNewReminder(cosmosWrapper, requestObject, updatedReminder, fileName, githubClient);
                }
                else
                {
                    existingReminder.Reminder = updatedReminder;
                    // recalculate next reminder due time from scratch:
                    existingReminder.NextReminder = new DateTime(updatedReminder.Date.Ticks, DateTimeKind.Utc);
                    if (existingReminder.LastReminder >= existingReminder.NextReminder)
                    {
                        // reminder start date is in the past, re-calculate next reminder due date with interval based on new start date.
                        existingReminder.CalculateNextReminder(DateTime.Now);
                    }

                    await cosmosWrapper.AddOrUpdateReminder(existingReminder);
                    await githubClient.CreateComment(requestObject.HeadCommit.Id,
                        $"Updated reminder '{updatedReminder.Title}' for {existingReminder.NextReminder:D}");
                }
            }

            await DeleteRemovedReminders(fileChanges.Deleted, cosmosWrapper, requestObject, githubClient);
        }

        async Task ValidateReminderDefinitions(FileChanges reminderChanges, CallbackModel requestObject,
            IGitHubRepository githubClient)
        {
            List<(string, ReminderDefinition)> newReminders;
            try
            {
                newReminders = await LoadReminders(reminderChanges.New, requestObject, githubClient);
                newReminders.AddRange(await LoadReminders(reminderChanges.Updated, requestObject, githubClient));
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

        bool IsGitCommitCallback(HttpRequest req)
        {
            if (!req.Headers.TryGetValue("X-GitHub-Event", out var callbackEvent) || callbackEvent != "push")
            {
                // Check for known callback types that we don't care
                if (callbackEvent != "check_suite") // ignore check_suite events
                {
                    // record unknown callback types to further analyze them
                    log.LogWarning($"Non-push callback. 'X-GitHub-Event': '{callbackEvent}'");
                }

                return false;
            }

            return true;
        }

        async Task CreateNewReminder(CosmosClientWrapper cosmosWrapper, CallbackModel requestObject, ReminderDefinition reminderDefinition, string fileName,
            IGitHubRepository githubClient)
        {
            var reminderDocument = ReminderDocument.New(
                requestObject.Installation.Id,
                requestObject.Repository.Id,
                fileName,
                reminderDefinition);

            await cosmosWrapper.AddOrUpdateReminder(reminderDocument);
            await githubClient.CreateComment(requestObject.HeadCommit.Id,
                $"Created reminder '{reminderDefinition.Title}' for {reminderDefinition.Date:D}");
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

        static async Task<List<(string, ReminderDefinition)>> LoadReminders(ICollection<string> filePaths, CallbackModel requestObject, IGitHubRepository installationClient)
        {
            var results = new List<(string, ReminderDefinition)>(filePaths.Count); // potentially lower but never higher than number of files
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

        async Task DeleteRemovedReminders(ICollection<string> deletedFiles, CosmosClientWrapper cosmosWrapper, CallbackModel requestObject, IGitHubRepository client)
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
                    await cosmosWrapper.Delete(deletedReminder, requestObject.Installation.Id,
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
