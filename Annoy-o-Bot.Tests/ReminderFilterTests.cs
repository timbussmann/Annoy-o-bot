using Annoy_o_Bot.GitHub.Callbacks;

namespace Annoy_o_Bot.Tests
{
    using System.Collections.Generic;
    using Xunit;

    public class ReminderFilterTests
    {
        [Fact]
        public void Should_only_return_files_in_reminders_folder()
        {
            var changes = new FileChanges
            {
                New = new HashSet<string>()
                {
                    ".reminders/new",
                    ".reminderses/new",
                    ".reminder/new",
                    "subfolder/new",
                    "new"
                },
                Deleted = new HashSet<string>
                {
                    "deleted",
                    ".reminders/deleted"
                },
                Updated = new HashSet<string>
                {
                    ".reminders/updated",
                    "updated"
                }
            };

            var result = ReminderFilter.FilterReminders(changes);

            var added = Assert.Single(result.New);
            Assert.Equal(".reminders/new", added);
            var deleted = Assert.Single(result.Deleted);
            Assert.Equal(".reminders/deleted", deleted);
            var modified = Assert.Single(result.Updated);
            Assert.Equal(".reminders/updated", modified);
        }
    }
}