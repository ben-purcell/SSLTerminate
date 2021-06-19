using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using SSLTerminate.ACME.Common;

namespace SSLTerminate.ACME.Tests.Utils
{
    class Response
    {
        public static HttpResponseMessage Json(int statusCode, object content)
        {
            var response = new HttpResponseMessage((HttpStatusCode) statusCode);

            var serialized = Common.Json.Serialize(content);

            response.Content = new StringContent(serialized);

            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            return response;
        }
    }
}
