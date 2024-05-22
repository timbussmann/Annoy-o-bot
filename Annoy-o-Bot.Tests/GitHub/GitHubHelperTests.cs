using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;
using Annoy_o_Bot.GitHub;

namespace Annoy_o_Bot.Tests
{
    public class GitHubHelperTests
    {
        [Theory]
        [InlineData("46F335537C051512C7554148D3683D98DEE8843E2E919A21065E0BD5FD09CDA5")]
        [InlineData("46f335537c051512c7554148d3683d98dee8843e2e919a21065e0bd5fd09cda5")]
        public async Task Should_verify_valid_body(string hash)
        {
            var gitHubApi = new GitHubApi(NullLoggerFactory.Instance);

            await gitHubApi.ValidateSignature("Hello World!", "secretkey", hash);
        }

        [Fact]
        public void Should_throw_on_invalid_body()
        {
            var gitHubApi = new GitHubApi(NullLoggerFactory.Instance);
            
            Assert.ThrowsAsync<Exception>(() => gitHubApi.ValidateSignature("Hello Wörld!", "secretkey", "B0D3E5FBD7B71A4539E27257AF48C677E8CAD2F803C2CC87C3164CD4254AFF79"));
        }
    }
}