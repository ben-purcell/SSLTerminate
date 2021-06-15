using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SSLTerminate.Whitelist
{
    static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAllowedHosts(
            this IServiceCollection services,
            Action<InMemoryWhitelistServiceConfig> options)
        {
            services.Configure(options);

            services.RemoveAll<IWhitelistService>();
            services.AddSingleton<IWhitelistService, FixedHostsWhitelistService>();

            return services;
        }
    }
}
