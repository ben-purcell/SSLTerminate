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
using SSLTerminate.Whitelist;

namespace SSLTerminate.Storage.Postgres
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresConnection(this IServiceCollection serviceCollection, Action<PostgresStorageOptions> options)
        {
            serviceCollection.Configure(options);
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresStores(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddPostgresAcmeAccountStore();
            serviceCollection.AddPostgresClientCertificateStore();
            serviceCollection.AddPostgresKeyAuthorizationsStore();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresAcmeAccountStore(this IServiceCollection serviceCollection)
        {
            serviceCollection.RemoveAll<IAcmeAccountStore>();
            serviceCollection.AddSingleton<IAcmeAccountStore, PostgresAcmeAccountStore>();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresClientCertificateStore(this IServiceCollection serviceCollection)
        {
            serviceCollection.RemoveAll<IClientCertificateStore>();
            serviceCollection.AddSingleton<IClientCertificateStore, PostgresClientCertificateStore>();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresKeyAuthorizationsStore(this IServiceCollection serviceCollection)
        {
            serviceCollection.RemoveAll<IKeyAuthorizationsStore>();
            serviceCollection.AddSingleton<IKeyAuthorizationsStore, PostgresKeyAuthorizationsStore>();
            return serviceCollection;
        }

        public static IServiceCollection AddPostgresWhitelist(this IServiceCollection serviceCollection)
        {
            serviceCollection.RemoveAll<IWhitelistService>();
            serviceCollection.AddSingleton<IWhitelistService, PostgresWhitelistService>();

            return serviceCollection;
        }
    }
}
