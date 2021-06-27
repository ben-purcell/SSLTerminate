using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SSLTerminate.ReverseProxy.Common.Services;
using Microsoft.AspNetCore.Proxy;
using SSLTerminate.ReverseProxy.Extensions;

namespace SSLTerminate.ReverseProxy.Services
{
    public class ReverseProxyHandler
    {
        private readonly IRegisteredRoutesService _registeredRoutesService;
        private readonly HttpMessageHandler _httpMessageHandler;

        public ReverseProxyHandler(
            IRegisteredRoutesService registeredRoutesService,
            HttpMessageHandler httpMessageHandler)
        {
            _registeredRoutesService = registeredRoutesService;
            _httpMessageHandler = httpMessageHandler;
        }

        public async Task Handle(HttpContext context)
        {
            var request = context.Request;

            var host = context.Request.Host.Host.ToLowerInvariant();

            var registeredRoute = await _registeredRoutesService.Get(host);

            if (registeredRoute == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var targetBase = new Uri(registeredRoute.Upstream);

            var targetUri = GetTargetUri(targetBase, request, (request.Path.HasValue, request.QueryString.HasValue));

            var proxiedRequest = context.Request.ProxyTo(targetUri);

            var client = new HttpClient(_httpMessageHandler);

            var responseMessage = await client.SendAsync(
                proxiedRequest,
                context.RequestAborted);

            if (responseMessage.StatusCode > 0)
            {
                await context.Response.CopyFromResponseMessage(
                    responseMessage,
                    context.RequestAborted);

                return;
            }

            context.Response.StatusCode = 502;
            await context.Response.WriteAsync("Internal Sever Error");
        }

        private Uri GetTargetUri(
            Uri baseUri, 
            HttpRequest request, 
            (bool hasPathString, bool hasQueryString) pathAndQueryStringInfo) => pathAndQueryStringInfo switch
        {
            (hasPathString: false, hasQueryString: false) => baseUri,
            (hasPathString: true, hasQueryString: false) => new Uri(baseUri, request.Path),
            (hasPathString: true, hasQueryString: true) => new Uri(baseUri, $"{request.Path}?{request.QueryString}"),
            (hasPathString: false, hasQueryString: true) => new Uri(baseUri, request.QueryString.ToString())
        };
    }
}
