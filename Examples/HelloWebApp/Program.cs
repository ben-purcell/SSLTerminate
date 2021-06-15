using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SSLTerminate;

namespace HelloWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .ConfigureKestrel(options =>
                        {
                            // serve the certificates from the ssl cert store
                            options.UseSslTerminateCertificates();
                        });
                });
    }
}
