using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SSLTerminate.CertificateLookup;
using SSLTerminate.Tests.Certs;
using SSLTerminate.Tests.Fakes;

namespace SSLTerminate.Tests.Unit.CertificateLookup
{
    public class CertificateFactoryTests
    {
        [Test]
        public async Task certificate_can_be_created()
        {
            var sslTerminateConfig = new SslTerminateConfig
            {
                AccountContacts = new [] { "test@mail.com" },
                AllowHosts = new [] { "host.com" },
                DirectoryUrl = "http://acme.com/directory",
                AcmeChallengePollFrequencySeconds = 0,
            };

            var (privateKey, pem) = DummyCertificate.CreateDummyPrivateKeyAndPem();

            var certificateRequestFactory = new FakeCertificateRequestFactory(privateKey, csr: new byte[] { 1, 2, 3, 4, 5});

            var happyPathAcmeClient = new HappyPathAcmeClient(pem);

            var acmeAccountStore = new FakeAcmeAccountStore();

            var keyAuthorizationsStore = new FakeKeyAuthorizationsStore();

            var factory = new CertificateFactory(
                acmeClientFactory: new FakeAcmeClientFactory(happyPathAcmeClient),
                acmeAccountStore: acmeAccountStore,
                keyAuthorizationsStore: keyAuthorizationsStore,
                certificateRequestFactory: certificateRequestFactory,
                config: Options.Create(sslTerminateConfig),
                logger: NullLogger<CertificateFactory>.Instance);

            var certificate = await factory.Create("host.com");

            certificate.Should().NotBeNull();
            acmeAccountStore.Keys.Should().NotBeNull();
        }
    }
}
