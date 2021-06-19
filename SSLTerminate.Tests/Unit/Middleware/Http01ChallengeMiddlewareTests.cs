using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SSLTerminate.Middleware;
using SSLTerminate.Tests.Fakes;

namespace SSLTerminate.Tests.Unit.Middleware
{
    public class Http01ChallengeMiddlewareTests
    {
        [Test]
        public async Task valid_challenge_can_be_handled()
        {
            var context = CreateTestContext(token: "token-123", keyAuthorization: "key-auth-123");

            await context.Middleware.Invoke(
                context.Http,
                keyAuthorizationsStore: context.KeyAuthorizationsStore,
                logger: NullLogger<Http01ChallengeMiddleware>.Instance);

            context.NextCalled().Should().BeFalse();
            context.Http.Response.StatusCode.Should().Be(200);
            context.Http.Response.ContentType.Should().Be("application/octet-stream");
            context.Http.Response.ContentLength.Should().Be("key-auth-123".Length);
            GetResponseBody(context.Http).Should().Be("key-auth-123");
        }

        [Test]
        public async Task https_requests_are_ignored()
        {
            var context = CreateTestContext(token: "token-123", keyAuthorization: "key-auth-123");

            context.Http.Request.IsHttps = true;

            await context.Middleware.Invoke(
                context.Http,
                keyAuthorizationsStore: context.KeyAuthorizationsStore,
                logger: NullLogger<Http01ChallengeMiddleware>.Instance);

            AssertRequestIgnored(context.NextCalled(), context.Http);
        }

        [TestCase("/other-path")]
        [TestCase("/.well-known/something-else/token123")]
        public async Task requests_to_other_paths_are_ignored(string path)
        {
            var context = CreateTestContext(token: "token-123", keyAuthorization: "key-auth-123");

            context.Http.Request.Path = path;

            await context.Middleware.Invoke(
                context.Http,
                keyAuthorizationsStore: context.KeyAuthorizationsStore,
                logger: NullLogger<Http01ChallengeMiddleware>.Instance);

            AssertRequestIgnored(context.NextCalled(), context.Http);
        }

        [TestCase("/.well-known/acme-challenge/token321")]
        [TestCase("/.well-known/acme-challenge/nonsense")]
        public async Task challenges_for_unknown_tokens_are_not_found(string path)
        {
            var context = CreateTestContext(token: "token-123", keyAuthorization: "key-auth-123");

            context.Http.Request.Path = path;

            await context.Middleware.Invoke(
                context.Http,
                keyAuthorizationsStore: context.KeyAuthorizationsStore,
                logger: NullLogger<Http01ChallengeMiddleware>.Instance);

            AssertResponse404(context.NextCalled(), context.Http);
        }

        private void AssertResponse404(bool nextCalled, DefaultHttpContext httpContext)
        {
            nextCalled.Should().BeFalse();
            httpContext.Response.StatusCode.Should().Be(404);
            GetResponseBody(httpContext).Should().Be(string.Empty);
        }

        private void AssertRequestIgnored(bool nextCalled, DefaultHttpContext httpContext)
        {
            nextCalled.Should().BeTrue();
            httpContext.Response.ContentLength.Should().BeNull();
            GetResponseBody(httpContext).Should().Be(string.Empty);
        }

        private static Http01ChallengeMiddlewareTestContext CreateTestContext(
            string token,
            string keyAuthorization)
        {
            var nextCalled = false;

            Task Next(HttpContext context)
            {
                nextCalled = true;
                return Task.CompletedTask;
            }

            var middleware = new Http01ChallengeMiddleware(Next);

            var httpContext = CreateHttpContextForChallengeRequest(token: "token-123");

            var keyAuthorizationsStore = new FakeKeyAuthorizationsStore()
                .With(token, keyAuthorization);

            return new Http01ChallengeMiddlewareTestContext
            {
                Http = httpContext,
                Middleware = middleware,
                KeyAuthorizationsStore = keyAuthorizationsStore,
                NextCalled = () => nextCalled
            };
        }

        private static DefaultHttpContext CreateHttpContextForChallengeRequest(string token)
        {
            var defaultHttpContext = new DefaultHttpContext();

            defaultHttpContext.Request.Method = "GET";
            defaultHttpContext.Request.Protocol = "http";
            defaultHttpContext.Request.Scheme = "http";
            defaultHttpContext.Request.IsHttps = false;
            defaultHttpContext.Request.Path = $"/.well-known/acme-challenge/{token}";

            defaultHttpContext.Response.Body = new MemoryStream();

            return defaultHttpContext;
        }

        private string GetResponseBody(DefaultHttpContext httpContext)
        {
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(httpContext.Response.Body);

            var responseBody = reader.ReadToEnd();

            return responseBody;
        }
    }

    internal class Http01ChallengeMiddlewareTestContext
    {
        public DefaultHttpContext Http { get; set; }
        public Http01ChallengeMiddleware Middleware { get; set; }
        public FakeKeyAuthorizationsStore KeyAuthorizationsStore { get; set; }
        public Func<bool> NextCalled { get; set; }
    }
}
