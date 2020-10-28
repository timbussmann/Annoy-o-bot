using Xunit;

namespace Annoy_o_Bot.Tests
{
    public class CommitParserTests
    {
        [Fact]
        public void GetDeletedReminders_should_only_return_deleted_files_in_reminder_folder()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel()
                {
                    Removed = new[]
                    {
                        "jsonInRootFolder.json",
                        ".reminders/markdownInReminders.md",
                        ".reminders/fileWithoutFiletype",
                        ".reminders/jsonInReminders.json",
                        "subfolder/jsonInSubfolder.json"
                    }
                }
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            Assert.Equal(3, result.Length);
            Assert.Contains(".reminders/jsonInReminders.json", result);
            Assert.Contains(".reminders/fileWithoutFiletype", result);
            Assert.Contains(".reminders/markdownInReminders.md", result);
        }

        [Fact]
        public void GetDeletedReminders_should_return_removed_files_from_all_commits()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel()
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders1.json",
                    }
                },
                new CallbackModel.CommitModel()
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders2.json",
                    }
                },
                new CallbackModel.CommitModel()
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders3.json",
                    }
                },
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            Assert.Equal(new[]
            {
                ".reminders/jsonInReminders1.json",
                ".reminders/jsonInReminders2.json",
                ".reminders/jsonInReminders3.json"
            }, result);
        }

        [Fact]
        public void GetDeletedReminders_should_not_return_reverted_removed_files()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            Assert.Empty(result);
        }

        [Fact]
        public void GetDeletedReminders_return_removed_files_with_complex_history()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            var file = Assert.Single(result);
            Assert.Equal(".reminders/jsonInReminders.json", file);
        }

        [Fact]
        public void GetDeletedReminders_should_return_removed_modified_files()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Modified = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            var file = Assert.Single(result);
            Assert.Equal(".reminders/jsonInReminders.json", file);
        }

        [Fact]
        public void GetDeletedReminders_should_not_return_reverted_reminders()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetDeletedReminders(commitModel);

            Assert.Empty(result);
        }

        [Fact]
        public void GetReminders_Should_only_return_files_in_reminder_folder()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel()
                {
                    Added = new[]
                    {
                        "jsonInRootFolder.json",
                        ".reminders/markdownInReminders.md",
                        ".reminders/fileWithoutFiletype",
                        ".reminders/jsonInReminders.json",
                        "subfolder/jsonInSubfolder.json"
                    }
                }
            };

            var result = CommitParser.GetReminders(commitModel);

            Assert.Equal(3, result.Length);
            Assert.Contains(".reminders/jsonInReminders.json", result);
            Assert.Contains(".reminders/fileWithoutFiletype", result);
            Assert.Contains(".reminders/markdownInReminders.md", result);

        }

        [Fact]
        public void GetReminders_Should_return_added_files_from_all_commits()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel()
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders1.json",
                    }
                },
                new CallbackModel.CommitModel()
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders2.json",
                    }
                },
                new CallbackModel.CommitModel()
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders3.json",
                    }
                },
            };

            var result = CommitParser.GetReminders(commitModel);

            Assert.Equal(new[]
            {
                ".reminders/jsonInReminders1.json",
                ".reminders/jsonInReminders2.json",
                ".reminders/jsonInReminders3.json"
            }, result);
        }

        [Fact]
        public void GetReminders_Should_not_return_reverted_new_files()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetReminders(commitModel);

            Assert.Empty(result);
        }

        [Fact]
        public void GetReminders_Should_not_return_deleted_modified_files()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Modified = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }
            };

            var result = CommitParser.GetReminders(commitModel);

            Assert.Empty(result);
        }

        [Fact]
        public void GetReminders_Should_return_added_reverted_added_again_file()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                }, 
                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/jsonInReminders.json",
                    }
                },
            };

            var result = CommitParser.GetReminders(commitModel);

            var file = Assert.Single(result);
            Assert.Equal(".reminders/jsonInReminders.json", file);
        }

        [Fact]
        public void GetReminders_should_include_modified_reminders()
        {
            var commitModel = new[]
            {

                new CallbackModel.CommitModel
                {
                    Added = new[]
                    {
                        ".reminders/newReminder.json",
                    },
                    Modified = new[]
                    {
                        ".reminders/existingReminder1.json"
                    }
                },
                new CallbackModel.CommitModel
                {
                    Removed = new[]
                    {
                        ".reminders/existingReminder1.json",
                    }
                },
                new CallbackModel.CommitModel
                {
                    Modified = new []
                    {
                        ".reminders/newReminder.json",
                        ".reminders/existingReminder2.json"
                    }
                },
            };

            var result = CommitParser.GetReminders(commitModel);

            Assert.Equal(new[]
            {
                ".reminders/newReminder.json",
                ".reminders/existingReminder2.json"
            }, result);
        }
    }
}