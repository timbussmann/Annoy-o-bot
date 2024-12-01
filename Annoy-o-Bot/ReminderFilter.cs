using Annoy_o_Bot.GitHub.Callbacks;

namespace Annoy_o_Bot
{
    public class ReminderFilter
    {
        const string reminderFilder = ".reminders/";

        public static FileChanges FilterReminders(FileChanges changes)
        {
            changes.Deleted.RemoveWhere(x => !x.StartsWith(reminderFilder));
            changes.Updated.RemoveWhere(x => !x.StartsWith(reminderFilder));
            changes.New.RemoveWhere(x => !x.StartsWith(reminderFilder));
            return changes;
        }
    }
}