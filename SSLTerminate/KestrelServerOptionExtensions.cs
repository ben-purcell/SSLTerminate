using System;
using System.Linq;
using System.Net.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SSLTerminate.CertificateLookup;

namespace SSLTerminate
{
    public static class HttpsConnectionAdapterOptionsExtensions
    {
        public static void UseSslTerminateCertificates(this KestrelServerOptions options)
        {
            AddHttpListener(options);

            AddHttpsListener(options);
        }

        private static void AddHttpListener(KestrelServerOptions options)
        {
            var httpUrls = GetListeningUrls("http");

            foreach (var httpUrl in httpUrls)
            {
                var portMatch = Regex.Match(httpUrl, @"(http):\/\/([^:]+):(\d+)");

                var httpPort = portMatch.Groups.Count == 4
                    ? int.Parse(portMatch.Groups[3].Value)
                    : 80;

                if (Uri.TryCreate(httpUrl, UriKind.Absolute, out var uri))
                    options.Listen(new UriEndPoint(uri));
                else
                    options.ListenAnyIP(httpPort);
            }
        }

        private static void AddHttpsListener(KestrelServerOptions options)
        {
            async ValueTask<SslServerAuthenticationOptions> LookupCertificate(SslStream stream, SslClientHelloInfo info, object state, CancellationToken token)
            {
                var certLookupService = options.ApplicationServices.GetRequiredService<ICertificateLookupService>();

                var config = options.ApplicationServices.GetRequiredService<IConfiguration>();

                var host = config["SSLTerminate:TestHost"] ?? info.ServerName;

                var certificate = await certLookupService.GetForHostAsync(host, token);

                return new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate
                };
            }

            var httpUrls = GetListeningUrls("https");

            foreach (var httpUrl in httpUrls)
            {
                var portMatch = Regex.Match(httpUrl, @"(https):\/\/([^:]+):(\d+)");

                var httpPort = portMatch.Groups.Count == 4
                    ? int.Parse(portMatch.Groups[3].Value)
                    : 443;

                if (Uri.TryCreate(httpUrl, UriKind.Absolute, out var uri))
                    options.Listen(new UriEndPoint(uri));
                else
                    options.ListenAnyIP(
                        httpPort, 
                        listenOptions => listenOptions.UseHttps(LookupCertificate,
                        state: new object(),
                        handshakeTimeout: TimeSpan.FromMinutes(1)));
            }
        }

        private static string[] GetListeningUrls(string scheme)
        {
            var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                ?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var schemeUrls = urls
                .Where(x => x.StartsWith($"{scheme}://", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return schemeUrls;
        }
    }
}
