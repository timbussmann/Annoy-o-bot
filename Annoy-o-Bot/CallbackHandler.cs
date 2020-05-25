using System;
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
            GitHubClient installationClient;
            CallbackModel requestObject;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(requestBody);
                requestObject = RequestParser.ParseJson(requestBody);

                if (!requestObject.Ref.EndsWith($"/{requestObject.Repository.DefaultBranch}"))
                {
                    // only react to commits to the main branch (e.g. master) as this is also the branch we read the reminder content from
                    return new OkResult();
                }

                installationClient = await GitHubHelper.GetInstallationClient(requestObject.Installation.Id);
            }
            catch (Exception e)
            {
                log.LogError(e, "Error at parsing callback input");
                throw;
            }

            foreach (var newFile in CommitParser.GetReminders(requestObject.Commits))
            {
                try
                {
                    var content = await installationClient.Repository.Content.GetAllContents(requestObject.Repository.Id, newFile);
                    var reminder = JsonReminderParser.Parse(content.First().Content);
                    await documents.AddAsync(new ReminderDocument
                    {
                        Id = BuildDocumentId(newFile),
                        InstallationId = requestObject.Installation.Id,
                        RepositoryId = requestObject.Repository.Id,
                        Reminder = reminder,
                        NextReminder = new DateTime(reminder.Date.Ticks, DateTimeKind.Utc),
                        Path = newFile
                    });
                    await CreateCommitComment($"Created reminder '{reminder.Title}' for {reminder.Date:D}");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to create reminder");
                    await CreateCommitComment($"Failed to create reminder {newFile}: {string.Join(Environment.NewLine, e.Message, e.StackTrace)}");
                    throw;
                }
            }

            var deletedReminders = CommitParser.GetDeletedReminders(requestObject.Commits);
            foreach (var deletedReminder in deletedReminders)
            {
                try
                {
                    var documentId = BuildDocumentId(deletedReminder);
                    var documentUri = UriFactory.CreateDocumentUri("annoydb", "reminders", documentId);
                    await documentClient.DeleteDocumentAsync(documentUri, new RequestOptions() { PartitionKey = new PartitionKey(documentId) });
                    await CreateCommitComment($"Deleted reminder '{deletedReminder}'");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Failed to delete reminder");
                    await CreateCommitComment(
                        $"Failed to delete reminder {deletedReminder}: {string.Join(Environment.NewLine, e.Message, e.StackTrace)}");
                    throw;
                }
                
            }

            return new OkResult();

            string BuildDocumentId(string fileName)
            {
                return $"{requestObject.Installation.Id}-{requestObject.Repository.Id}-{fileName.Split('/').Last()}";
            }

            Task CreateCommitComment(string comment)
            {
                return installationClient.Repository.Comment.Create(
                    requestObject.Repository.Id,
                    requestObject.HeadCommit.Id,
                    new NewCommitComment(comment));
            }
        }
    }

    public class ReminderDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public Reminder Reminder { get; set; }
        public long InstallationId { get; set; }
        public long RepositoryId { get; set; }
        public DateTime LastReminder { get; set; }
        public DateTime NextReminder { get; set; }
        public string Path { get; set; }
    }

    public class Reminder
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Assignee { get; set; }
        public DateTime Date { get; set; }
        public Interval Interval { get; set; }
        public int? IntervalStep { get; set; }
    }

    public enum Interval
    {
        Once = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }


    //TODO: support projects

}
