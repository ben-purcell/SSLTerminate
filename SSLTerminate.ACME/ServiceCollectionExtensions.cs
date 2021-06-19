using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SSLTerminate.ACME.Exceptions;

[assembly: InternalsVisibleTo("SSLTerminate.ACME.Tests")]
namespace SSLTerminate.ACME
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddAcmeServices(
            this IServiceCollection services,
            Action<AcmeConfig> options = null,
            Func<HttpMessageHandler> httpMessageHandlerFactory = null)
        {
            if (options == null)
            {
                options = opts => opts.DirectoryUrl = Directories.LetsEncrypt.Staging;
            }

            if (httpMessageHandlerFactory == null)
                httpMessageHandlerFactory = () => new HttpClientHandler();

            services.Configure(options);

            services.AddSingleton<IAcmeClientFactory>(x =>
            {
                var opts = x.GetService<IOptions<AcmeConfig>>()?.Value ?? throw new AcmeException("Missing configuration");

                var factory = new AcmeClientFactory(opts.DirectoryUrl, httpMessageHandlerFactory(), x.GetService<ILogger<AcmeClient>>());

                return factory;
            });

            return services;
        }
    }
}
