namespace Annoy_o_Bot.GitHub.Callbacks;

public static class CallbackModelExtensions
{
    public static bool IsDefaultBranch(this GitPushCallbackModel gitPushCallbackModel)
    {
        return gitPushCallbackModel.Ref.EndsWith($"/{gitPushCallbackModel.Repository.DefaultBranch}");
    }
}