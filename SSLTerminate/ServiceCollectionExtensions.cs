using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using SSLTerminate.ACME;
using SSLTerminate.CertificateLookup;
using SSLTerminate.Stores;
using SSLTerminate.Utils;
using SSLTerminate.Whitelist;

[assembly: InternalsVisibleTo("SSLTerminate.Tests")]
namespace SSLTerminate
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSslTerminate(
            this IServiceCollection services, Action<SslTerminateConfig> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.Configure(options);

            var config = new SslTerminateConfig();
            options(config);

            services.AddAcmeServices(opts =>
            {
                opts.DirectoryUrl = config.DirectoryUrl ?? Directories.LetsEncrypt.Staging;
            });

            services.AddTransient<ICertificateLookupService, CertificateLookupService>();
            services.AddTransient<ICertificateFactory, CertificateFactory>();
            services.AddTransient<ICertificateRequestFactory, CertificateRequestFactory>();

            AddHostCheck(services, config);

            AddStores(services);

            return services;
        }

        private static void AddHostCheck(IServiceCollection services, SslTerminateConfig config)
        {
            services.AddAllowedHosts(opts =>
            {
                opts.AllowedHosts = config.AllowHosts;
            });
        }

        private static void AddStores(IServiceCollection services)
        {
            services.AddFileSystemKeyAuthorizationsStore(opts =>
            {
                opts.KeyAuthorizationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stores", "key-authz");
            });

            services.AddFileSystemCertificateStore(opts =>
            {
                opts.ClientCertificatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stores", "client-certs");
            });

            services.AddFileSystemAccountStore(opts =>
            {
                opts.AcmeAccountPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stores", "acme-account.json");
            });
        }
    }
}
