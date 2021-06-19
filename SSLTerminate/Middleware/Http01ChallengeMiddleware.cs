using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Middleware
{
    class Http01ChallengeMiddleware
    {
        private readonly RequestDelegate _next;

        public Http01ChallengeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext httpContext, 
            IKeyAuthorizationsStore keyAuthorizationsStore,
            ILogger<Http01ChallengeMiddleware> logger)
        {
            logger.LogDebug($"{nameof(Http01ChallengeMiddleware)}: Request received: {httpContext.Request.Path}");

            if (httpContext.Request.IsHttps)
            {
                logger.LogDebug("HTTPS detected, ignoring request");
                await _next.Invoke(httpContext);
                return;
            }

            if (httpContext.Request.Method.ToUpperInvariant() != "GET")
            {
                logger.LogDebug("Non GET request, ignoring request");
                await _next.Invoke(httpContext);
                return;
            }

            var path = httpContext.Request.Path.Value;
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogDebug("No path found, ignoring request");
                await _next.Invoke(httpContext);
                return;
            }

            var match = Regex.Match(
                input: path,
                pattern: @"^\/?\.well\-known\/acme\-challenge/(?<token>.+)$",
                options: RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                logger.LogDebug("Path not ACME challenge, ignoring request");
                await _next.Invoke(httpContext);
                return;
            }

            logger.LogInformation("ACME challenge detected");

            var response = httpContext.Response;

            var token = match.Groups["token"].Value;
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogDebug($"Token not found: {token}");

                response.StatusCode = 404;
                return;
            }

            logger.LogDebug($"Token found: {token}");

            var keyAuthorization = await keyAuthorizationsStore.GetKeyAuthorization(token);
            if (keyAuthorization == null)
            {
                logger.LogInformation($"No key authorization found for token: {token}");

                response.StatusCode = 404;
                return;
            }

            logger.LogInformation($"Key authorization found, token: {token}   ---   keyAuth: {keyAuthorization}");

            var keyAuthBytes = Encoding.ASCII.GetBytes(keyAuthorization);

            response.ContentLength = keyAuthBytes.Length;
            response.ContentType = "application/octet-stream";
            response.StatusCode = 200;
            await response.Body.WriteAsync(keyAuthBytes, httpContext.RequestAborted);
        }
    }
}