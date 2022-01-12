namespace Annoy_o_Bot.Tests
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Unicode;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Xunit;

    public class GitHubHelperTests
    {
        [Fact]
        public void Should_verify_valid_body()
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext());
            request.Headers.Add("X-Hub-Signature-256", "sha256=B0D3E5FBD7B71A4539E27257AF48C677E8CAD2F803C2CC87C3164CD4254AFF79");
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Hello World!"));
            
            var hmac = new HMACSHA256(new byte[]{ 1, 3, 4, 8, 7, 2, 6});
            GitHubHelper.ValidateRequest(request, hmac);
        }

        [Fact]
        public void Should_throw_on_invalid_body()
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext());
            request.Headers.Add("X-Hub-Signature-256", "sha256=B0D3E5FBD7B71A4539E27257AF48C677E8CAD2F803C2CC87C3164CD4254AFF79");
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Hello Wörld!"));
            
            var hmac = new HMACSHA256(new byte[] { 1, 3, 4, 8, 7, 2, 6 });
            Assert.Throws<Exception>(() => GitHubHelper.ValidateRequest(request, hmac));
        }
    }
}