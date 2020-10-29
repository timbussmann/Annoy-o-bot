using System.Collections.Generic;
using System.Linq;

namespace Annoy_o_Bot
{
    public class CommitParser
    {
        public static string[] GetReminders(CallbackModel.CommitModel[] commits)
        {
            commits = SelectRelevantCommits(commits);

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
            commits = SelectRelevantCommits(commits);

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

        private static CallbackModel.CommitModel[] SelectRelevantCommits(CallbackModel.CommitModel[] commits)
        {
            // if the last commit is a merge commit, ignore other commits as the merge commits contains all the relevant changes
            // TODO: This behavior will be incorrect if a non-merge-commit contains this commit message. To be absolutely sure, we'd have to retrieve the full commit object and inspect the parent information. This information is not available on the callback object
            if (commits.LastOrDefault()?.Message?.StartsWith("Merge ") ?? false)
            {
                commits = commits.TakeLast(1).ToArray();
            }

            return commits;
        }
    }
}