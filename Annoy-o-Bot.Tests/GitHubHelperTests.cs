﻿using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Headers.Add("X-Hub-Signature-256", $"sha256={hash}");
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Hello World!"));

            await GitHubHelper.ValidateRequest(request, "secretkey", NullLogger.Instance);
        }

        [Fact]
        public void Should_throw_on_invalid_body()
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Headers.Add("X-Hub-Signature-256", "sha256=B0D3E5FBD7B71A4539E27257AF48C677E8CAD2F803C2CC87C3164CD4254AFF79");
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Hello Wörld!"));

            Assert.ThrowsAsync<Exception>(() => GitHubHelper.ValidateRequest(request, "somekey", NullLogger.Instance));
        }
    }
}