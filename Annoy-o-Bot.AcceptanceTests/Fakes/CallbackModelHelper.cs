using Annoy_o_Bot.GitHub.Callbacks;

namespace Annoy_o_Bot.AcceptanceTests.Fakes;

class CallbackModelHelper
{
    public static GitPushCallbackModel.CommitModel CreateCommitModel(string? added = null, string? modified = null, string? removed = null)
    {
        var commit = new GitPushCallbackModel.CommitModel
        {
            Id = Guid.NewGuid().ToString(),
            Added = added != null ? new[] { added } : Array.Empty<string>(),
            Modified = modified != null ? new[] { modified } : Array.Empty<string>(),
            Removed = removed != null ? new[] { removed } : Array.Empty<string>()
        };

        return commit;
    }
}