using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SSLTerminate;
using SSLTerminate.ACME;

namespace HelloWebApp
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
                options.AccountContacts = new[]
                {
                    _configuration["SSLTerminate:AccountEmail"]
                };

                options.AllowHosts = _configuration
                    .GetSection("SSLTerminate:AllowedHosts")
                    .Get<string[]>();

                options.DirectoryUrl = Directories.LetsEncrypt.Staging;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // listen for http 01 challenges where necessary
            app.UseHttp01ChallengeHandler();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello Web App!");
                });
            });
        }
    }
}