using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using SSLTerminate.ACME;
using SSLTerminate.ReverseProxy.Common;
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

            services.AddPostgresStores(options =>
            {
                options.ConnectionString = _configuration["SSLTerminate:ConnectionString"];
            });

            services.AddPostgresWhitelist(options =>
            {
                options.ConnectionString = _configuration["SSLTerminate:ConnectionString"];
            });
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
                var mode = _configuration.GetValue("Mode", Modes.ReverseProxy);

                if (mode == Modes.ManageRoutes)
                {
                    endpoints.MapControllerRoute(
                        "RoutesManagement",
                        "/api/routes");
                }
                else
                {
                    // reverse proxy goes here
                }

                endpoints.MapGet("/$/alive", async context =>
                {
                    await context.Response.WriteAsync("I'm here");
                });
            });
        }
    }
}
