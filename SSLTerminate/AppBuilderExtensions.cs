using Microsoft.AspNetCore.Builder;
using SSLTerminate.Middleware;

namespace SSLTerminate
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseHttp01ChallengeHandler(this IApplicationBuilder appBuilder)
        {
            return appBuilder.UseMiddleware<Http01ChallengeMiddleware>();
        }
    }
}
