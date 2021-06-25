using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SSLTerminate.Stores.AcmeAccounts;
using SSLTerminate.Stores.ClientCertificates;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Storage.Postgres
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresStorage(
            this IServiceCollection serviceCollection,
            Action<PostgresStorageOptions> options)
        {
            serviceCollection.AddPostgresAcmeAccountStore(options);
            serviceCollection.AddPostgresClientCertificateStore(options);
            serviceCollection.AddPostgresKeyAuthorizationsStore(options);
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresAcmeAccountStore(
            this IServiceCollection serviceCollection,
            Action<PostgresStorageOptions> options)
        {
            serviceCollection.Configure(options);

            serviceCollection.RemoveAll<IAcmeAccountStore>();
            serviceCollection.AddSingleton<IAcmeAccountStore, PostgresAcmeAccountStore>();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresClientCertificateStore(
            this IServiceCollection serviceCollection,
            Action<PostgresStorageOptions> options)
        {
            serviceCollection.Configure(options);

            serviceCollection.RemoveAll<IClientCertificateStore>();
            serviceCollection.AddSingleton<IClientCertificateStore, PostgresClientCertificateStore>();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresKeyAuthorizationsStore(
            this IServiceCollection serviceCollection,
            Action<PostgresStorageOptions> options)
        {
            serviceCollection.Configure(options);

            serviceCollection.RemoveAll<IKeyAuthorizationsStore>();
            serviceCollection.AddSingleton<IKeyAuthorizationsStore, PostgresKeyAuthorizationsStore>();
            return serviceCollection;
        }
    }
}
