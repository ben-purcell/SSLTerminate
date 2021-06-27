using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SSLTerminate.ReverseProxy.Common.Entities;
using SSLTerminate.ReverseProxy.Common.Services;
using SSLTerminate.ReverseProxy.Tests.Fakes;

namespace SSLTerminate.ReverseProxy.Tests.Integration
{
    public class ReverseProxyTests
    {
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("SSLTERMINATE_POSTGRES_TEST_CSTRING") ??
                                                          "User ID=postgres;Password=password;Host=localhost;Port=5439;Database=postgres;Pooling=true;";

        [Test]
        public async Task reverse_proxy_keep_alive_works()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetAsync("/$/alive");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task reverse_proxy_handles_get()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetStringAsync("http://www.blah.com");

            response.Should().Be("some string content");
        }

        [Test]
        public async Task reverse_proxy_handles_get_with_path()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetStringAsync("http://www.blah.com/string");

            response.Should().Be("some string content");
        }

        [Test]
        public async Task reverse_proxy_handles_not_found()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetAsync("http://notfound.com");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task reverse_proxy_handles_unreachable()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetAsync("http://unreachable.com");

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [Test]
        public async Task reverse_proxy_handles_unknown_route()
        {
            var client = CreateTestHttpClient();

            var response = await client.GetAsync("http://not-registered-route.com");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task reverse_proxy_handles_post()
        {
            var client = CreateTestHttpClient();

            var response = await client.PostAsJsonAsync("http://www.blah.com/post-here", new 
            {
                One = 1,
                Two = 2,
                Three = "a string"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        private static HttpClient CreateTestHttpClient()
        {
            var testConfig = new Dictionary<string, string>
            {
                ["SSLTerminate:ConnectionString"] = ConnectionString
            };

            var server = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddInMemoryCollection(testConfig);
                })
                .UseStartup<SSLTerminate.ReverseProxy.Startup>()
                .ConfigureTestServices(serviceCollection =>
                {
                    serviceCollection.RemoveAll<HttpMessageHandler>();
                    serviceCollection.AddSingleton<HttpMessageHandler, FakeHttpClient>();
                })
            );

            var client = server.CreateClient();

            PopulateRegisteredRoutes(server);

            return client;
        }

        private static void PopulateRegisteredRoutes(TestServer server)
        {
            var registeredRoutesService = server.Services.GetRequiredService<IRegisteredRoutesService>();

            registeredRoutesService.Add(new RegisteredRoute
            {
                Host = "www.blah.com",
                Upstream = "http://blah.app.com"
            });

            registeredRoutesService.Add(new RegisteredRoute
            {
                Host = "notfound.com",
                Upstream = "http://notfound.ever.com"
            });

            registeredRoutesService.Add(new RegisteredRoute
            {
                Host = "unreachable.com",
                Upstream = "http://unreachable.app.com"
            });
        }
    }
}
