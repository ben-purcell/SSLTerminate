using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Stores.AcmeAccounts;
using SSLTerminate.Stores.ClientCertificates;
using SSLTerminate.Stores.KeyAuthorizations;
using SSLTerminate.Tests.Common.certs;

namespace SSLTerminate.Storage.Postgres.Tests
{
    public class Tests
    {
        [Test]
        public async Task postgres_acme_account_store_crud_works()
        {
            var services = CreatePostgresStoreServices();

            var acmeAccountStore = services.GetRequiredService<IAcmeAccountStore>();

            var accountKeys = new AcmeAccountKeys
            {
                KeyId = "12345",
                PrivateKey = new RsaPrivateKey()
            };

            await acmeAccountStore.Store(accountKeys);

            var stored = await acmeAccountStore.Get();

            stored.KeyId.Should().Be("12345");
            stored.PrivateKey.Should().BeEquivalentTo(accountKeys.PrivateKey);
        }

        [Test]
        public async Task postgres_key_auth_store_and_retrieve_works()
        {
            var services = CreatePostgresStoreServices();

            var keyAuthStore = services.GetRequiredService<IKeyAuthorizationsStore>();

            await keyAuthStore.Store("123", "456");

            var stored = await keyAuthStore.GetKeyAuthorization("123");

            stored.Should().Be("456");

            await keyAuthStore.Remove("123");

            stored = await keyAuthStore.GetKeyAuthorization("123");

            stored.Should().BeNull();
        }

        [Test]
        public async Task postgres_client_cert_store_crud_works()
        {
            var services = CreatePostgresStoreServices();

            var clientCertStore = services.GetRequiredService<IClientCertificateStore>();

            var certificateWithPrivateKey = DummyCertificate.One();

            await clientCertStore.Store("www.blah.com", certificateWithPrivateKey);

            var stored = await clientCertStore.GetCertificateWithPrivateKey("www.blah.com");

            stored.RawData.Should().BeEquivalentTo(certificateWithPrivateKey.RawData);
            stored.PrivateKey.ExportPkcs8PrivateKey().Should().BeEquivalentTo(
                certificateWithPrivateKey.PrivateKey.ExportPkcs8PrivateKey());
        }

        private static ServiceProvider CreatePostgresStoreServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddPostgresStorage(x =>
            {
                x.ConnectionString = "";
            });

            serviceCollection
                .AddTransient(typeof(ILogger<>), typeof(NullLogger<>));

            var provider = serviceCollection.BuildServiceProvider();

            return provider;
        }
    }
}