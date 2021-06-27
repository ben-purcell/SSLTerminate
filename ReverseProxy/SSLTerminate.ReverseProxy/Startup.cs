using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SSLTerminate.ACME;
using SSLTerminate.ReverseProxy.Common;
using SSLTerminate.ReverseProxy.Services;
using SSLTerminate.Storage.Postgres;

namespace SSLTerminate.ReverseProxy
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddSslTerminate(options =>
            {
                options.DirectoryUrl = _configuration.GetValue("SSLTerminate:Directory:UseProd", false)
                    ? Directories.LetsEncrypt.Production
                    : Directories.LetsEncrypt.Staging;

                options.AccountContacts = new []
                {
                    _configuration["SSLTerminate:Contact"]
                };
            });

            services
                .AddPostgresConnection(options => options.ConnectionString = _configuration["SSLTerminate:ConnectionString"])
                .AddPostgresStores()
                .AddPostgresWhitelist()
                .AddReverseProxyCommonServices();

            services
                .AddTransient<HttpMessageHandler, HttpClientHandler>()
                .AddSingleton<ReverseProxyHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttp01ChallengeHandler();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/$/alive", async context =>
                {
                    await context.Response.WriteAsync("I'm here");
                });
            });

            app.Run(async context =>
            {
                var handler = context
                    .RequestServices
                    .GetRequiredService<ReverseProxyHandler>();

                await handler.Handle(context);
            });
        }
    }
}
