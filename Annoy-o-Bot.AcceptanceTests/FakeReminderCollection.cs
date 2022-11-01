using Microsoft.Azure.WebJobs;

namespace Annoy_o_Bot.AcceptanceTests;

class FakeReminderCollection : IAsyncCollector<ReminderDocument>
{
    public List<ReminderDocument> AddedDocuments { get; set; } = new List<ReminderDocument>();

    public Task AddAsync(ReminderDocument item, CancellationToken cancellationToken = new CancellationToken())
    {
        AddedDocuments.Add(item);
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }
}