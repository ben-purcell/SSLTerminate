using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SSLTerminate.Stores.AcmeAccounts;
using SSLTerminate.Stores.ClientCertificates;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Stores
{
    static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileSystemCertificateStore(
            this IServiceCollection services,
            Action<FileSystemClientCertificateStoreConfig> options)
        {
            services.Configure(options);

            services.RemoveAll<IClientCertificateStore>();
            services.AddSingleton<IClientCertificateStore, FileSystemClientCertificateStore>();

            return services;
        }

        public static IServiceCollection AddFileSystemAccountStore(
            this IServiceCollection services, 
            Action<FileSystemAcmeAccountStoreConfig> options)
        {
            services.Configure(options);

            services.RemoveAll<IAcmeAccountStore>();
            services.AddSingleton<IAcmeAccountStore, FileSystemAcmeAccountStore>();

            return services;
        }

        public static IServiceCollection AddFileSystemKeyAuthorizationsStore(
            this IServiceCollection services, Action<FileSystemKeyAuthorizationConfig> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.Configure(options);

            services.RemoveAll<IKeyAuthorizationsStore>();
            services.AddTransient<IKeyAuthorizationsStore, FileSystemKeyAuthorizationsStore>();

            return services;
        }
    }

    
}
