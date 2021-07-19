using System.Collections.Generic;
using System.Linq;

namespace Annoy_o_Bot
{
    public class FileChanges
    {
        public HashSet<string> New { get; set; } = new HashSet<string>();
        public HashSet<string> Updated { get; set; } = new HashSet<string>();
        public HashSet<string> Deleted { get; set; } = new HashSet<string>();
    }

    public class CommitParser
    {
        public static FileChanges GetChanges(CallbackModel.CommitModel[] commits)
        {
            var changes = new FileChanges();

            foreach (var commit in commits)
            {
                foreach (var added in commit.Added)
                {
                    if (changes.Deleted.Contains(added))
                    {
                        changes.Deleted.Remove(added);
                        changes.Updated.Add(added);
                    }
                    else
                    {
                        changes.Updated.Remove(added);
                        changes.New.Add(added);
                    }
                }

                foreach (var modified in commit.Modified)
                {
                    changes.Deleted.Remove(modified);
                    if (!changes.New.Contains(modified))
                    {
                        changes.Updated.Add(modified);
                    }
                }

                foreach (var removed in commit.Removed)
                {
                    changes.New.Remove(removed);
                    changes.Updated.Remove(removed);
                    changes.Deleted.Add(removed);
                }
            }

            return changes;
        }

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