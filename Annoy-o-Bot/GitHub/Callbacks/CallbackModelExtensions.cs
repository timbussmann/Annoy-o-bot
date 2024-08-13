namespace Annoy_o_Bot.GitHub.Callbacks;

public static class CallbackModelExtensions
{
    public static bool IsDefaultBranch(this CallbackModel callbackModel)
    {
        return callbackModel.Ref.EndsWith($"/{callbackModel.Repository.DefaultBranch}");
    }
}