using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Common;

namespace SSLTerminate.ACME
{
    class AcmeClientFactory : IAcmeClientFactory
    {
        private readonly string _directoryUrl;
        private readonly HttpMessageHandler _httpMessageHandler;
        private readonly ILogger<AcmeClient> _logger;

        public AcmeClientFactory(string directoryUrl, HttpMessageHandler httpMessageHandler, ILogger<AcmeClient> logger)
        {
            _directoryUrl = directoryUrl;
            _httpMessageHandler = httpMessageHandler;
            _logger = logger;
        }

        public async Task<IAcmeClient> Create()
        {
            var httpClient = new HttpClient(_httpMessageHandler);

            var directory = await GetAcmeDirectory(httpClient);

            var client = new AcmeClient(_httpMessageHandler, directory, _logger);

            return client;
        }

        private async Task<AcmeDirectory> GetAcmeDirectory(HttpClient client)
        {
            _logger.LogDebug($"Retrieving ACME directory from: '{_directoryUrl}'");

            var directory = await client.GetFromJsonAsync<AcmeDirectory>(_directoryUrl, Json.JsonSerializerOptions);

            _logger.LogDebug($"Directory retrieved");

            return directory;
        }
    }
}