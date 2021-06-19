using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SSLTerminate.ACME.Common;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME.JWS
{
    static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> SendJwsAsync(
            this HttpClient client,
            string url,
            IPrivateKey privateKey,
            string nonce,
            object payload,
            CancellationToken cancellationToken)
        {
            var jws = Jws.Create(privateKey, nonce, url, payload);

            return await SendAsync(client, url, jws, cancellationToken);
        }

        public static async Task<(HttpResponseMessage, T)> SendJwsAsync<T>(
            this HttpClient client, 
            string url, 
            IPrivateKey privateKey,
            string nonce,
            object payload,
            CancellationToken cancellationToken)
        {
            var jws = Jws.Create(privateKey, nonce, url, payload);

            return await SendAsync<T>(client, url, jws, cancellationToken);
        }

        /// If payload is null, the request will be done POST-as-GET
        /// (payload sent as empty string)
        public static async Task<(HttpResponseMessage, T)> SendJwsAsync<T>(
            this HttpClient client,
            string url,
            IPrivateKey privateKey,
            string keyId,
            string nonce,
            object payload,
            CancellationToken cancellationToken)
        {
            var jws = Jws.Create(privateKey, keyId, nonce, url, payload);

            return await SendAsync<T>(client, url, jws, cancellationToken);
        }

        public static async Task<HttpResponseMessage> SendJwsAsync(
            this HttpClient client,
            string url,
            IPrivateKey privateKey,
            string keyId,
            string nonce,
            object payload,
            CancellationToken cancellationToken)
        {
            var jws = Jws.Create(privateKey, keyId, nonce, url, payload);

            return await SendAsync(client, url, jws, cancellationToken);
        }

        private static async Task<(HttpResponseMessage, T)> SendAsync<T>(
            HttpClient client, 
            string url, 
            Jws jws,
            CancellationToken cancellationToken)
        {
            var response = await SendAsync(client, url, jws, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"Invalid status code: {response.StatusCode}, error: {responseContent}";
                throw new JwsRequestException(errorMessage);
            }

            var deserialized = Json.Deserialize<T>(responseContent);

            return (response, deserialized);
        }

        private static async Task<HttpResponseMessage> SendAsync(
            HttpClient client, 
            string url, 
            Jws jws,
            CancellationToken cancellationToken)
        {
            var serialized = JsonSerializer.Serialize(jws, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            });

            var content = new StringContent(serialized);
            content.Headers.Clear();
            content.Headers.TryAddWithoutValidation("Content-Type", "application/jose+json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            var response = await client.SendAsync(request, cancellationToken);

            return response;
        }
    }
}
