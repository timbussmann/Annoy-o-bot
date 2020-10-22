using System.IO;
using Xunit;

namespace Annoy_o_Bot.Tests
{
    public class RequestParserTests
    {
        [Fact]
        public void NewFileAdded()
        {
            var result = RequestParser.ParseJson(File.ReadAllText("requests/fileAdded.json"));

            Assert.Equal("refs/heads/master", result.Ref);

            Assert.Equal(7498230L, result.Installation.Id);

            Assert.Equal(179716425L, result.Repository.Id);
            Assert.Equal("master", result.Repository.DefaultBranch);

            var commit = Assert.Single(result.Commits);
            Assert.Equal("ba7c6f17f5beaafc603eca52b864356848865fec", commit.Id);
            var addedFile = Assert.Single(commit.Added);
            Assert.Equal(".reminder/testReminder.json", addedFile);

            Assert.Equal("timbussmann", result.Pusher.Name);
            
        }

        [Fact]
        public void MultiCommit()
        {
            var result = RequestParser.ParseJson(File.ReadAllText("requests/multiCommitFileHistory.json"));

            Assert.Equal(4, result.Commits.Length);

            Assert.Equal(".reminder/newFile.json", Assert.Single(result.Commits[0].Added));
            Assert.Empty(result.Commits[0].Modified);
            Assert.Empty(result.Commits[0].Removed);

            Assert.Equal(".reminder/newFile.json", Assert.Single(result.Commits[1].Modified));
            Assert.Empty(result.Commits[1].Added);
            Assert.Empty(result.Commits[1].Removed);

            Assert.Equal(".reminder/newFile.json", Assert.Single(result.Commits[2].Removed));
            Assert.Empty(result.Commits[2].Added);
            Assert.Empty(result.Commits[2].Modified);

            Assert.Empty(result.Commits[3].Added);
            Assert.Empty(result.Commits[3].Modified);
            Assert.Empty(result.Commits[3].Removed);

            Assert.Equal("cb1ec97f51657c2718ab4e0b1d0bf2656aeb3127", result.HeadCommit.Id);
        }

        [Fact]
        public void BranchDeleted()
        {
            var result = RequestParser.ParseJson(File.ReadAllText("requests/branchDeleted.json"));

            Assert.Null(result.HeadCommit);
            Assert.Empty(result.Commits);
        }
    }
}