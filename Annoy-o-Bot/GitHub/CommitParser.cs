using System.Collections.Generic;

namespace Annoy_o_Bot.GitHub
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
    }
}