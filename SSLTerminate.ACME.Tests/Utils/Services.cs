using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SSLTerminate.ACME.Tests.Fakes;

namespace SSLTerminate.ACME.Tests.Utils
{
    public class Services
    {
        public static IServiceProvider CreateProvider(Action<IServiceCollection> services)
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }
    }
}
