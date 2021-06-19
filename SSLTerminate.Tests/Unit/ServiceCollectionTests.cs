using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SSLTerminate.CertificateLookup;
using SSLTerminate.Stores.AcmeAccounts;
using SSLTerminate.Stores.ClientCertificates;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Tests.Unit
{
    public class ServiceCollectionTests
    {
        [Test]
        public void CertificateLookupService_can_be_created()
        {
            var provider = CreateSslTerminateServices();

            var certificateLookupService = provider.GetService<ICertificateLookupService>();

            certificateLookupService.Should().NotBeNull();
        }

        [Test]
        public void default_key_authz_store_is_file_system()
        {
            var provider = CreateSslTerminateServices();

            var keyAuthzStore = provider.GetService<IKeyAuthorizationsStore>();

            keyAuthzStore.Should().BeOfType<FileSystemKeyAuthorizationsStore>();
        }

        [Test]
        public void default_client_certs_store_is_file_system()
        {
            var provider = CreateSslTerminateServices();

            var certsStore = provider.GetService<IClientCertificateStore>();

            certsStore.Should().BeOfType<FileSystemClientCertificateStore>();
        }

        [Test]
        public void default_acme_account_store_is_file_system()
        {
            var provider = CreateSslTerminateServices();

            var acmeAccountStore = provider.GetService<IAcmeAccountStore>();

            acmeAccountStore.Should().BeOfType<FileSystemAcmeAccountStore>();
        }

        [Test]
        public void default_acme_challenge_poll_freqency_is_10_seconds()
        {
            var provider = CreateSslTerminateServices();

            var options = provider.GetRequiredService<IOptions<SslTerminateConfig>>();

            options.Value.AcmeChallengePollFrequencySeconds.Should().Be(10);
        }

        private static ServiceProvider CreateSslTerminateServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSslTerminate(x =>
            {
                x.AccountContacts = new[] {"one@blah.com", "two@blah.com"};
                x.AllowHosts = new[] {"hosta.com", "hostb.com"};
                x.DirectoryUrl = "acme.com/directory";
            });

            serviceCollection
                .AddTransient(typeof(ILogger<>), typeof(NullLogger<>));

            var provider = serviceCollection.BuildServiceProvider();

            return provider;
        }
    }
}
