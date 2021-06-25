using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SSLTerminate.CertificateLookup;
using SSLTerminate.Exceptions;
using SSLTerminate.Tests.Common.certs;
using SSLTerminate.Tests.Fakes;
using SSLTerminate.Whitelist;

namespace SSLTerminate.Tests.Unit.CertificateLookup
{
    public class CertificateLookupTests
    {
        [Test]
        public async Task existing_certificate_from_store_is_served()
        {
            var dummyCertificate = DummyCertificate.One();

            var (certificateLookupService, _) = await CreateCertificateLookupService(
                allowedHosts: new[] { "host.com" },
                hostsWithCerts: new [] { ("host.com", certificate: dummyCertificate) });

            var certificate = await certificateLookupService.GetForHostAsync("host.com");

            certificate.Should().Be(dummyCertificate);
        }

        [Test]
        public async Task certificate_can_be_created_for_whitelisted_host()
        {
            var newCertificate = DummyCertificate.Two();

            var fakeCertificateFactory = new FakeCertificateFactory()
                .ThatCreates(host: "another-host.com", certificate: newCertificate);

            var (certificateLookupService, clientCertificateStore) = await CreateCertificateLookupService(
                allowedHosts: new[] { "host.com", "another-host.com" },
                hostsWithCerts: new[] { ("host.com", certificate: DummyCertificate.One()) },
                certificateFactory: fakeCertificateFactory);

            var certificate = await certificateLookupService.GetForHostAsync("another-host.com");

            certificate.Should().Be(newCertificate);
            clientCertificateStore.GetCertificate("another-host.com").Should().Be(newCertificate);
        }

        [Test]
        public async Task if_host_not_whitelisted_then_exception_thrown()
        {
            var (certificateLookupService, _) = await CreateCertificateLookupService(
                allowedHosts: new[] { "host.com" },
                hostsWithCerts: new[] { ("host.com", certificate: DummyCertificate.One()) });

            Func<Task> getCert = async () => await certificateLookupService.GetForHostAsync("different-host.com");

            getCert.Should().Throw<HostNotAllowedException>();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task doesnt_allow_null_or_whitespace_hosts(string host)
        {
            var (certificateLookupService, _) = await CreateCertificateLookupService(
                allowedHosts: new[] { "host.com" },
                hostsWithCerts: new[] { ("host.com", certificate: DummyCertificate.One()) });

            Func<Task> getCert = async () => await certificateLookupService.GetForHostAsync(host);

            getCert.Should().Throw<HostMissingException>();
        }

        private static async Task<(CertificateLookupService, FakeClientCertificateStore)> CreateCertificateLookupService(
            string[] allowedHosts,
            (string host, X509Certificate2 certificate)[] hostsWithCerts,
            ICertificateFactory certificateFactory = null
        )
        {
            var fixedHostsConfig = new FixedHostsWhitelistServiceConfig
            {
                AllowedHosts = allowedHosts
            };

            var fixedHostsWhitelistService = new FixedHostsWhitelistService(
                Options.Create(fixedHostsConfig),
                NullLogger<FixedHostsWhitelistService>.Instance);

            var clientCertificateStore = new FakeClientCertificateStore();

            foreach (var (host, cert) in hostsWithCerts)
            {
                await clientCertificateStore.Store(host, cert);
            }

            certificateFactory ??= new FakeCertificateFactory();

            var certificateLookupService = new CertificateLookupService(
                whitelistService: fixedHostsWhitelistService,
                clientCertificateStore: clientCertificateStore,
                certificateFactory: certificateFactory);

            return (certificateLookupService, clientCertificateStore);
        }
    }
}
