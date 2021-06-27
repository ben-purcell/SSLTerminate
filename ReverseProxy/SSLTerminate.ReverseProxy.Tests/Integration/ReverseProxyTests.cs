using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

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

        private static HttpClient CreateTestHttpClient()
        {
            var testConfig = new Dictionary<string, string>
            {
                ["SSLTerminate:ConnectionString"] = ConnectionString
            };

            var server = new TestServer(new WebHostBuilder()
                .ConfigureAppConfiguration(x => { x.AddInMemoryCollection(testConfig); })
                .UseStartup<SSLTerminate.ReverseProxy.Startup>());

            var client = server.CreateClient();
            return client;
        }
    }
}
