using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Exceptions;
using SSLTerminate.ACME.JWS;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME
{
    class AcmeClient : IAcmeClient
    {
        private readonly AcmeDirectory _directory;
        private readonly ILogger<AcmeClient> _logger;
        private readonly HttpClient _client;

        public AcmeClient(
            HttpMessageHandler httpMessageHandler,
            AcmeDirectory directory,
            ILogger<AcmeClient> logger)
        {
            _directory = directory;
            _logger = logger;
            _client = new HttpClient(httpMessageHandler);
        }

        public async Task<AcmeAccountResponse> CreateAccount(
            AcmeAccountRequest acmeAccountRequest,
            IPrivateKey privateKey,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Creating account using '{_directory.NewAccount}'...");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (response, accountResponse) = await _client.SendJwsAsync<AcmeAccountResponse>(
                _directory.NewAccount,
                privateKey,
                nonce,
                acmeAccountRequest,
                cancellationToken);

            accountResponse.Location = response.Headers.Location?.AbsoluteUri;

            _logger.LogInformation("Account created: " + accountResponse.Location);

            return accountResponse;
        }

        public async Task<AcmeOrderResponse> CreateOrder(
            AcmeAccountKeys keys, 
            AcmeOrderRequest orderRequest,
            CancellationToken cancellationToken = default)
        {

            _logger.LogInformation($"Creating order using '{_directory.NewOrder}'...");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (httpResponse, orderResponse) = await _client.SendJwsAsync<AcmeOrderResponse>(
                _directory.NewOrder,
                keys.PrivateKey,
                keys.KeyId, 
                nonce,
                orderRequest,
                cancellationToken);

            orderResponse.Location = httpResponse.Headers.Location?.AbsoluteUri;

            _logger.LogInformation("Order created: " + orderResponse.Location);

            return orderResponse;
        }

        public async Task<AcmeAuthorizationsResponse> GetAuthorizations(
            AcmeAccountKeys keys, 
            string authorizationUrl,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Getting authorizations using '{authorizationUrl}'...");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (_, authorizations) = await _client.SendJwsAsync<AcmeAuthorizationsResponse>(
                authorizationUrl, 
                keys.PrivateKey, 
                keys.KeyId, 
                nonce,
                payload: Nothing,
                cancellationToken);

            _logger.LogInformation("Authorizations retrieved: " + authorizationUrl);

            return authorizations;
        }

        public async Task<AcmeChallengeResponse> SignalReadyForChallenge(
            AcmeAccountKeys keys, 
            AcmeChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Signalling challenge ready using '{challenge.Url}'...");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (_, challengeResponse) = await _client.SendJwsAsync<AcmeChallengeResponse>(
                challenge.Url,
                keys.PrivateKey,
                keys.KeyId,
                nonce,
                payload: EmptyObject,
                cancellationToken);

            _logger.LogInformation("Challenge ready sent: " + challenge.Url);

            return challengeResponse;
        }

        public async Task<AcmeOrderResponse> GetOrder(
            AcmeAccountKeys keys, 
            string orderLocation,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Retrieving order from '{orderLocation}'...");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (response, orderResponse) = await _client.SendJwsAsync<AcmeOrderResponse>(
                orderLocation,
                keys.PrivateKey,
                keys.KeyId,
                nonce,
                payload: Nothing,
                cancellationToken);

            orderResponse.Location = response.Headers.Location?.AbsoluteUri;

            _logger.LogInformation("Order retrieved: " + orderLocation);

            return orderResponse;
        }

        public async Task<AcmeOrderResponse> Finalize(
            AcmeAccountKeys accountKeys, 
            string orderFinalizeUrl, 
            AcmeFinalizeRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Finalizing order: {orderFinalizeUrl}");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var (response, orderResponse) = await _client.SendJwsAsync<AcmeOrderResponse>(
                orderFinalizeUrl,
                accountKeys.PrivateKey,
                accountKeys.KeyId,
                nonce,
                request,
                cancellationToken);

            orderResponse.Location = response.Headers.Location?.AbsoluteUri;

            _logger.LogInformation($"Finalized order: {orderFinalizeUrl}, status: {orderResponse.Status}");

            return orderResponse;
        }

        public async Task<string> DownloadCertificate(
            AcmeAccountKeys accountKeys, 
            string certificateUrl,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Downloading certificate: {certificateUrl}");

            var nonce = await GetNonce(_client, _directory, cancellationToken);

            var response = await _client.SendJwsAsync(
                certificateUrl,
                accountKeys.PrivateKey,
                accountKeys.KeyId,
                nonce,
                payload: Nothing,
                cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = response.Content.ReadAsStringAsync(cancellationToken);

                throw new AcmeException(
                    $"Unable to download certificate '{certificateUrl}' - response status: {response.StatusCode} - response content: {content}");
            }

            var pemChain = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation($"Certificate downloaded: {certificateUrl}");

            return pemChain;
        }

        private async Task<string> GetNonce(
            HttpClient client, 
            AcmeDirectory directory,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting nonce...");

            var request = new HttpRequestMessage(HttpMethod.Head, directory.NewNonce);

            var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new AcmeException("Unable to retrieve nonce, url: " + directory.NewNonce);

            var nonceHeaders = response
                .Headers
                .Where(x => x.Key.Equals("replay-nonce", StringComparison.OrdinalIgnoreCase));

            foreach (var header in nonceHeaders)
            {
                _logger.LogInformation("Nonce retrieved");

                return header.Value.FirstOrDefault();
            }

            throw new AcmeException("Unable to find nonce, url: " + directory.NewNonce);
        }

        private static object EmptyObject => new object();

        // send payload containing nothing to achieve POST-as-GET
        // this is treated differently to an empty object
        private object Nothing => null;
    }
}