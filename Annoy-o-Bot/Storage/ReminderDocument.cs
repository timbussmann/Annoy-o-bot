using System;
using Newtonsoft.Json;

namespace Annoy_o_Bot
{
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
}