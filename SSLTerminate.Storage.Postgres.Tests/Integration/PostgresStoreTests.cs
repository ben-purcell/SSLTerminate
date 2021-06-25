using System;
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

namespace SSLTerminate.Storage.Postgres.Tests.Integration
{
    public class Tests
    {
        private static readonly string ConnectionString = Environment.GetEnvironmentVariable("SSLTERMINATE_POSTGRES_TEST_CSTRING") ??
                                                           "User ID=postgres;Password=password;Host=localhost;Port=5435;Database=postgres;Pooling=true;";

        [SetUp]
        public void SetUp()
        {
            Db.DropStores(ConnectionString);
            Db.CreateStores(ConnectionString);
        }

        [Test]
        public async Task postgres_acme_account_store_crud_works()
        {
            var services = CreatePostgresStoreServices();

            var acmeAccountStore = (PostgresAcmeAccountStore) services.GetRequiredService<IAcmeAccountStore>();

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
        public async Task postgres_acme_account_can_be_null()
        {
            var services = CreatePostgresStoreServices();

            var acmeAccountStore = (PostgresAcmeAccountStore)services.GetRequiredService<IAcmeAccountStore>();

            var acmeAccountKeys = await acmeAccountStore.Get();

            acmeAccountKeys.Should().BeNull();
        }

        [Test]
        public async Task postgres_key_auth_store_and_retrieve_works()
        {
            var services = CreatePostgresStoreServices();

            var keyAuthStore = (PostgresKeyAuthorizationsStore) services.GetRequiredService<IKeyAuthorizationsStore>();

            await keyAuthStore.Store("123", "456");

            var stored = await keyAuthStore.GetKeyAuthorization("123");

            stored.Should().Be("456");

            await keyAuthStore.Remove("123");

            stored = await keyAuthStore.GetKeyAuthorization("123");

            stored.Should().BeNull();
        }

        [Test]
        public async Task postgres_key_auth_store_can_return_null()
        {
            var services = CreatePostgresStoreServices();

            var keyAuthStore = (PostgresKeyAuthorizationsStore) services.GetRequiredService<IKeyAuthorizationsStore>();

            var auth = await keyAuthStore.GetKeyAuthorization("doesn't-exist");

            auth.Should().BeNull();
        }

        [Test]
        public async Task postgres_client_cert_store_crud_works()
        {
            var services = CreatePostgresStoreServices();

            var clientCertStore = (PostgresClientCertificateStore) services.GetRequiredService<IClientCertificateStore>();

            var certificateWithPrivateKey = DummyCertificate.One();

            await clientCertStore.Store("www.blah.com", certificateWithPrivateKey);

            var stored = await clientCertStore.GetCertificateWithPrivateKey("www.blah.com");

            stored.RawData.Should().BeEquivalentTo(certificateWithPrivateKey.RawData);
            stored.PrivateKey.ExportPkcs8PrivateKey().Should().BeEquivalentTo(
                certificateWithPrivateKey.PrivateKey.ExportPkcs8PrivateKey());
        }

        [Test]
        public async Task postgres_client_certificate_store_can_return_null()
        {
            var services = CreatePostgresStoreServices();

            var clientCertificateStore = (PostgresClientCertificateStore)services.GetRequiredService<IClientCertificateStore>();

            var certificate = await clientCertificateStore.GetCertificateWithPrivateKey("www.nonsense.com");

            certificate.Should().BeNull();
        }

        private static ServiceProvider CreatePostgresStoreServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddPostgresStorage(x =>
            {
                x.ConnectionString = ConnectionString;
            });

            serviceCollection
                .AddTransient(typeof(ILogger<>), typeof(NullLogger<>));

            var provider = serviceCollection.BuildServiceProvider();

            return provider;
        }
    }
}