using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using NUnit.Framework;
using SSLTerminate.ReverseProxy.Common.Entities;
using SSLTerminate.ReverseProxy.Management.Models;
using SSLTerminate.Storage.Postgres;

namespace SSLTerminate.ReverseProxy.Tests.Integration
{
    public class RegisteredRouteManagementTests
    {
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("SSLTERMINATE_POSTGRES_TEST_CSTRING") ??
                                                          "User ID=postgres;Password=password;Host=localhost;Port=5439;Database=postgres;Pooling=true;";

        [SetUp]
        public void SetUp()
        {
            Db.DropTables(ConnectionString);
            Db.CreateTables(ConnectionString);

            SSLTerminate.ReverseProxy.Common.Db.DropTables(ConnectionString);
            SSLTerminate.ReverseProxy.Common.Db.CreateTables(ConnectionString);
        }

        [Test]
        public async Task route_crud_works()
        {
            var client = CreateTestHttpClient();

            var addRouteResponse = await client.PostAsJsonAsync("/routes", new AddRegisteredRouteRequest
            {
                Host = "www.blah.com",
                Redirect = "blah.main.com"
            });

            var checkRouteExistsResponse = await client.GetFromJsonAsync<RegisteredRoute>("/routes?host=www.blah.com");

            var deletedResponse = await client.DeleteAsync("/routes?host=www.blah.com");

            var getAfterDelete = await client.GetAsync("/routes?host=www.blah.com");

            addRouteResponse.IsSuccessStatusCode.Should().BeTrue();

            checkRouteExistsResponse.Host.Should().Be("www.blah.com");
            checkRouteExistsResponse.Redirect.Should().Be("blah.main.com");

            deletedResponse.IsSuccessStatusCode.Should().BeTrue();
            getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task route_management_keep_alive_works()
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
                .UseStartup<SSLTerminate.ReverseProxy.Management.Startup>());

            var client = server.CreateClient();
            return client;
        }
    }
}
