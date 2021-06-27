using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTerminate.ReverseProxy.Tests.Fakes
{
    class FakeHttpClient : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            var handler = GetHandler((request.Method.Method, request.RequestUri.AbsoluteUri));

            var response = handler();

            return Task.FromResult(response);
        }

        private Func<HttpResponseMessage> GetHandler((string method, string uri) request) => request switch
        {
            ("GET", "http://blah.app.com/") => HandleGet,
            ("GET", "http://blah.app.com/string") => HandleGet,
            ("GET", "http://blah.app.com/json") => HandleGetJson,
            ("POST", "http://blah.app.com/post-here") => HandlePost,
            ("GET", "http://notfound.ever.com/") => NotFound,
            _ => Unreachable
        };

        private HttpResponseMessage HandleGet()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("some string content")
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            return response;
        }

        private HttpResponseMessage HandleGetJson()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    One = 1,
                    Two = "two"
                }))
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return response;
        }

        private HttpResponseMessage HandlePost()
        {
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        private HttpResponseMessage NotFound()
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            };
        }

        private HttpResponseMessage Unreachable()
        {
            return new HttpResponseMessage(0);
        }
    }
}
