using System.Collections.Generic;
using System.Linq;

namespace Annoy_o_Bot
{
    public class CommitParser
    {
        public static string[] GetReminders(CallbackModel.CommitModel[] commits)
        {
            return commits.Aggregate(new HashSet<string>(), (added, model) =>
            {
                foreach (var newFile in model.Added)
                {
                    added.Add(newFile);
                }

                foreach (var modifiedFile in model.Modified)
                {
                    added.Add(modifiedFile);
                }

                foreach (var remvovedFile in model.Removed)
                {
                    added.Remove(remvovedFile);
                }

                return added;
            })
                .Where(x => x.StartsWith(".reminders/") && x.EndsWith(".json"))
                .ToArray();
        }

        public static string[] GetDeletedReminders(CallbackModel.CommitModel[] commits)
        {
            return commits.Aggregate((removed: new HashSet<string>(), @new: new HashSet<string>()), (tuple, model) =>
                {
                    foreach (var newFile in model.Added)
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

                    foreach (var remvovedFile in model.Removed)
                    {
                        if (!tuple.@new.Contains(remvovedFile))
                        {
                            tuple.removed.Add(remvovedFile);
                        }
                    }

                    return tuple;
                })
                .removed
                .Where(x => x.StartsWith(".reminders/") && x.EndsWith(".json"))
                .ToArray();

        }
    }
}