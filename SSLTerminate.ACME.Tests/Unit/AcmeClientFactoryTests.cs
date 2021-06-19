using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Keys;
using SSLTerminate.ACME.Tests.Fakes;
using SSLTerminate.ACME.Tests.Utils;

namespace SSLTerminate.ACME.Tests.Unit
{
    public class AcmeClientFactoryTests
    {
        [Test]
        public void AcmeClientFactory_can_be_created()
        {
            var acmeServer = new FakeAcmeServer();

            var serviceProvider = Services.CreateProvider(services =>
            {
                services.AddAcmeServices(httpMessageHandlerFactory: () => acmeServer);
            });

            var factory = serviceProvider.GetService<IAcmeClientFactory>();

            Assert.That(factory, Is.Not.Null);
        }

        [Test]
        public async Task AcmeClient_can_be_created()
        {
            var acmeServer = new FakeAcmeServer();

            var serviceProvider = Services.CreateProvider(services =>
            {
                services.AddAcmeServices(
                    options: x => x.DirectoryUrl = FakeAcmeServer.DirectoryUrl,
                    httpMessageHandlerFactory: () => acmeServer);
            });

            var factory = serviceProvider.GetService<IAcmeClientFactory>();

            var client = await factory.Create();

            Assert.That(client, Is.Not.Null);
        }
    }
}