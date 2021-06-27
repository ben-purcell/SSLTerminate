using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SSLTerminate.ACME;
using SSLTerminate.ReverseProxy.Common.Services;
using SSLTerminate.Storage.Postgres;

namespace SSLTerminate.ReverseProxy.Common
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReverseProxyCommonServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRegisteredRoutesService, RegisteredRoutesService>();
            serviceCollection.AddTransient<IRegisteredRouteRepository, PostgresRegisteredRouteRepository>();

            return serviceCollection;
        }
    }
}
