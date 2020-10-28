using System.Collections.Generic;
using System.Linq;

namespace Annoy_o_Bot
{
    public class CommitParser
    {
        public static string[] GetReminders(CallbackModel.CommitModel[] commits)
        {
            return commits.Aggregate(new HashSet<string>(), (added, commit) =>
            {
                foreach (var newFile in commit.Added)
                {
                    added.Add(newFile);
                }

                foreach (var modifiedFile in commit.Modified)
                {
                    added.Add(modifiedFile);
                }

                foreach (var remvovedFile in commit.Removed)
                {
                    added.Remove(remvovedFile);
                }

                return added;
            })
                .Where(x => x.StartsWith(".reminders/"))
                .ToArray();
        }

        public static string[] GetDeletedReminders(CallbackModel.CommitModel[] commits)
        {
            return commits.Aggregate((removed: new HashSet<string>(), @new: new HashSet<string>()), (tuple, commit) =>
                {
                    foreach (var newFile in commit.Added)
                    {
                        if (tuple.removed.Contains(newFile))
                        {
                            tuple.removed.Remove(newFile);
                        }
                        else
                        {
                            tuple.@new.Add(newFile);
                        }
                    }

                    foreach (var remvovedFile in commit.Removed)
                    {
                        if (!tuple.@new.Contains(remvovedFile))
                        {
                            tuple.removed.Add(remvovedFile);
                        }
                    }

                    return tuple;
                })
                .removed
                .Where(x => x.StartsWith(".reminders/"))
                .ToArray();

        }
    }
}