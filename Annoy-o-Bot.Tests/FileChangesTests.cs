namespace Annoy_o_Bot.Tests
{
    using Xunit;

    public class FileChangesTests
    {
        [Fact]
        public void Should_return_empty_changes_when_no_commits()
        {
            var result = CommitParser.GetChanges(new CallbackModel.CommitModel[0]);

            Assert.Empty(result.New);
            Assert.Empty(result.Deleted);
            Assert.Empty(result.Updated);
        }

        [Fact]
        public void Should_aggregate_changes_from_all_commits()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Added = new []{ "a1" },
                    Modified = new []{ "m1" },
                    Removed = new [] { "r1"}
                },
                new CallbackModel.CommitModel
                {
                    Added = new []{ "a2" },
                    Modified = new []{ "m2" },
                    Removed = new [] { "r2"}
                },
                new CallbackModel.CommitModel
                {
                    Added = new []{ "a3" },
                    Modified = new []{ "m3" },
                    Removed = new [] { "r3"}
                },
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Contains("a1", result.New);
            Assert.Contains("a2", result.New);
            Assert.Contains("a3", result.New);
            Assert.Contains("m1", result.Updated);
            Assert.Contains("m2", result.Updated);
            Assert.Contains("m3", result.Updated);
            Assert.Contains("r1", result.Deleted);
            Assert.Contains("r2", result.Deleted);
            Assert.Contains("r3", result.Deleted);
        }

        [Fact]
        public void Should_handle_multiple_updates()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Modified = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Modified = new [] { "file1"}
                }
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Equal(1, result.Updated.Count);
            Assert.Contains("file1", result.Updated);
        }

        [Fact]
        public void Should_handle_update_deletes()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Modified = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Removed = new [] { "file1"}
                }
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Empty(result.Updated);
            var deleted = Assert.Single(result.Deleted);
            Assert.Contains("file1", deleted);
        }

        [Fact]
        public void Should_handle_new_delete()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Added = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Modified = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Removed = new [] { "file1"}
                }
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Equal(0, result.Updated.Count);
            Assert.Equal(0, result.New.Count);
            Assert.Equal(1, result.Deleted.Count);
            Assert.Contains("file1", result.Deleted);
        }

        [Fact]
        public void Should_handle_new_update()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Added = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Modified = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Modified = new [] { "file1"}
                }
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Equal(0, result.Updated.Count);
            Assert.Equal(0, result.Deleted.Count);
            Assert.Equal(1, result.New.Count);
            Assert.Contains("file1", result.New);
        }

        [Fact]
        public void Should_handle_delete_new()
        {
            var commitModel = new[]
            {
                new CallbackModel.CommitModel
                {
                    Modified = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Removed = new []{ "file1" },
                },
                new CallbackModel.CommitModel
                {
                    Added = new [] { "file1"}
                }
            };

            var result = CommitParser.GetChanges(commitModel);

            Assert.Empty(result.Deleted);
            Assert.Empty(result.New);
            var updated = Assert.Single(result.Updated);
            Assert.Equal("file1", updated);
        }
    }
}