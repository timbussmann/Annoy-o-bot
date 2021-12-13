#nullable disable

using Newtonsoft.Json;

namespace Annoy_o_Bot
{
    public class CallbackModel
    {
        public InstallationModel Installation { get; set; }
        public RepositoryModel Repository { get; set; }
        public CommitModel[] Commits { get; set; }
        public string Ref { get; set; }
        [JsonProperty("head_commit")]
        public CommitModel HeadCommit { get; set; }
        public PusherModel Pusher { get; set; }

        public class CommitModel
        {
            public string Id { get; set; }
            public string Message { get; set; }
            public string[] Added { get; set; } = new string[0];
            public string[] Modified { get; set; } = new string[0];
            public string[] Removed { get; set; } = new string[0];
        }

        public class InstallationModel
        {
            public long Id { get; set; }
        }

        public class RepositoryModel
        {
            public long Id { get; set; }
            [JsonProperty("default_branch")]
            public string DefaultBranch { get; set; }
            public string Name { get; set; }
        }

        public class PusherModel
        {
            public string Name { get; set; }
        }
    }
}