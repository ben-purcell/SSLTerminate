using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Common;
using SSLTerminate.ACME.JWS;
using SSLTerminate.ACME.Keys;
using SSLTerminate.ACME.Tests.Utils;

namespace SSLTerminate.ACME.Tests.Fakes
{
    public class FakeAcmeServer : HttpMessageHandler
    {
        public static readonly AcmeDirectory Directory = new AcmeDirectory
        {
            NewAccount = $"{BaseUrl}/new-account",
            NewNonce = $"{BaseUrl}/new-nonce",
            NewOrder = $"{BaseUrl}/new-order",
            KeyChange = $"{BaseUrl}/key-change",
            RevokeCert = $"{BaseUrl}/revoke-cert"
        };

        public const string BaseUrl = "http://acme.local";

        public const string DirectoryUrl = BaseUrl + "/directory";

        private readonly Random _random = new Random();

        private readonly Dictionary<(string url, HttpMethod method), Func<HttpRequestMessage, Task<HttpResponseMessage>>> _handlers;

        private readonly HashSet<string> _issuedNonces = new HashSet<string>();
        private readonly HashSet<string> _usedNonces = new HashSet<string>();

        private readonly Dictionary<string, Jwk> _createdAccounts = new Dictionary<string, Jwk>();
        public HashSet<string> CreatedAccounts { get; } = new HashSet<string>();

        private readonly Dictionary<string, AcmeOrderResponse> _orders = new Dictionary<string, AcmeOrderResponse>();
        private readonly Dictionary<string, AcmeChallenge> _challenges = new Dictionary<string, AcmeChallenge>();

        public DateTime Now { get; set; } = new DateTime(2020, 3, 1, 12, 56, 01);

        public DateTime OrderExpiry { get; set; } = new DateTime(2020, 4, 1, 12, 56, 01);

        private string RandomString => _random.Next(1, 999999).ToString();

        public FakeAcmeServer()
        {
            _handlers = new Dictionary<(string url, HttpMethod method), Func<HttpRequestMessage, Task<HttpResponseMessage>>>
            {
                [(DirectoryUrl, HttpMethod.Get)] = HandleDirectoryRequest,
                [(Directory.NewNonce, HttpMethod.Head)] = HandleNonceRequest,
                [(Directory.NewAccount, HttpMethod.Post)] = HandleAccountRequest,
                [(Directory.NewOrder, HttpMethod.Post)] = HandleNewOrderRequest,
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            var key = (request.RequestUri.AbsoluteUri.ToLowerInvariant(), request.Method);

            var canHandle = _handlers.TryGetValue(key, out var handler);

            if (canHandle)
                return handler(request);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private Task<HttpResponseMessage> HandleDirectoryRequest(HttpRequestMessage request)
        {
            return Task.FromResult(Response.Json(200, Directory));
        }

        private Task<HttpResponseMessage> HandleNonceRequest(HttpRequestMessage request)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var nonce = _random.Next(123400, 9999999).ToString();

            response.Headers.Add("replay-nonce", nonce);

            _issuedNonces.Add(nonce);

            return Task.FromResult(response);
        }

        private async Task<HttpResponseMessage> HandleAccountRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureJwkPresent: true);

            var payload = Deserialize.FromBase64Url<AcmeAccountRequest>(jws.Payload);

            var response = Response.Json(200, new AcmeAccountResponse
            {
                Contact = payload.Contact,
                Status = AcmeAccountStatuses.Valid,
                CreatedAt = Now,
            });

            var account = _random.Next(1, 999999);

            var accountLocation = $"{BaseUrl}/acme/acct/{account}";

            var @protected = Deserialize.FromBase64Url<ProtectedHeader>(jws.Protected);

            AddAccount(accountLocation, @protected.Jwk);

            response.Headers.Location = new Uri(accountLocation);

            return response;
        }

        private async Task<HttpResponseMessage> HandleNewOrderRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            var payload = Deserialize.FromBase64Url<AcmeOrderRequest>(jws.Payload);

            var order = _random.Next(1, 999999);

            var orderLocation = $"{BaseUrl}/acme/acct/{order}";

            var acmeOrderResponse = new AcmeOrderResponse
            {
                Identifiers = payload.Identifiers,
                Status = AcmeOrderStatus.Pending,
                Authorizations = new []
                {
                    $"{BaseUrl}/test-auth/{order}"
                },
                Finalize = $"{BaseUrl}/test-finalize/{order}",
                Expires = OrderExpiry,
                NotBefore = Now,
                NotAfter = OrderExpiry
            };

            AddOrder(orderLocation, acmeOrderResponse);

            var response = Response.Json(201, acmeOrderResponse);
            response.Headers.Location = new Uri(orderLocation);

            return response;
        }

        private async Task<HttpResponseMessage> HandleRetrieveOrderRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            Validate.IsPostAsGet(jws);

            var order = _orders[request.RequestUri.AbsoluteUri];

            var response = Response.Json(201, order);
            response.Headers.Location = new Uri(request.RequestUri.AbsoluteUri);

            return response;
        }

        private async Task<HttpResponseMessage> HandleAuthorizationsRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            Validate.IsPostAsGet(jws);

            var order = GetOrderByAuthorizationUrlOrThrow(request.RequestUri.AbsoluteUri);

            var response = new AcmeAuthorizationsResponse
            {
                Status = AcmeOrderStatus.Pending,
                Expires = OrderExpiry,
                Identifier = new AcmeIdentifier { Type = order.Identifiers[0].Type, Value = order.Identifiers[0].Value },
                Challenges = new []
                {
                    new AcmeChallenge { Type = "http-01", Url = $"{BaseUrl}/acme/chall/{RandomString}", Token = RandomString },
                    new AcmeChallenge { Type = "dns-01", Url = $"{BaseUrl}/acme/chall/{RandomString}", Token = RandomString },
                },
            };

            foreach (var challenge in response.Challenges)
                AddChallenge(challenge);

            return Response.Json(200, response);
        }

        private async Task<HttpResponseMessage> HandleChallengeRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            Validate.IsEmptyJsonMessage(jws);

            var challenge = _challenges[request.RequestUri.AbsoluteUri];

            return Response.Json(200, new AcmeChallengeResponse
            {
                Status = "pending",
                Url = challenge.Url,
                Token = challenge.Token,
                Type = challenge.Type
            });
        }

        private async Task<HttpResponseMessage> HandleFinalizeRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            var payload = Deserialize.FromBase64Url<AcmeFinalizeRequest>(jws.Payload);

            var order = GetOrderFromFinalizeOrThrow(request.RequestUri.AbsoluteUri);

            var response = Response.Json(200, order);

            response.Headers.Location = new Uri(order.Location);

            return response;
        }

        private const string Pem = 
@"-----BEGIN TRUSTED CERTIFICATE-----
MIICFjCCAX8CFFESViEhotR6sypAo1xd3IAOKZ37MA0GCSqGSIb3DQEBCwUAMEox
CzAJBgNVBAYTAkdCMRgwFgYDVQQIDA9Ob3R0aW5naGFtc2hpcmUxITAfBgNVBAoM
GEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDAeFw0yMTA2MTEyMzQxNTVaFw0yMjA2
MTEyMzQxNTVaMEoxCzAJBgNVBAYTAkdCMRgwFgYDVQQIDA9Ob3R0aW5naGFtc2hp
cmUxITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDCBnzANBgkqhkiG
9w0BAQEFAAOBjQAwgYkCgYEArGUw72exeX9xSTu9FoiqgI9+z9Ob80uUyvzwwywM
Ua3px9iDP6C5VNgHKOiPmhg63g33A2aPqjH6iZZivRV7YA+E8tR/HjN1Qbv09n7b
2bJrWqFuElqpR9KdlkCXDGcQjY1c4hM9udi30qP4wXBZfxiPgy8ZleLb2+BKBBvq
pkECAwEAATANBgkqhkiG9w0BAQsFAAOBgQCDiuqN3gZTOQY+Ch94xLyHliRyU4Di
NlibbG9TEMo96iftsdlVIq1M02w+MwJjGvQEELBEDrn48z60JWC0p7TyE/+77HYh
HbO7JzrF+Qtgraatcficos8po91fQYQ0N4PzxjrYfX0+w7/NxyFC0Y+W0b2Y8j6w
AZ8DHXSUW1Bjaw==
-----END TRUSTED CERTIFICATE-----";
        private async Task<HttpResponseMessage> HandleDownloadCertificateRequest(HttpRequestMessage request)
        {
            CheckHeaders(request);

            var jws = await ValidateAndGetJws(request.Content, ensureKeyIdPresent: true);

            Validate.IsPostAsGet(jws);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = new StringContent(Pem)
            };

            return response;
        }

        private AcmeOrderResponse GetOrderFromFinalizeOrThrow(string finalizeUrl)
        {
            var order = _orders
                .Select(x => x.Value)
                .FirstOrDefault(x => x.Finalize == finalizeUrl);

            return order ?? throw new ApplicationException("Unable to find order by finalizeUrl: " + finalizeUrl);
        }

        private AcmeOrderResponse GetOrderByAuthorizationUrlOrThrow(string authorizationUrl)
        {
            var order = _orders
                .Select(x => x.Value)
                .FirstOrDefault(x => x.Authorizations.Any(a => a == authorizationUrl));

            return order ?? throw new ApplicationException("Unknown authorizationUrl: " + authorizationUrl);
        }

        private void CheckHeaders(HttpRequestMessage request)
        {
            Validate.JoseJsonContentType(request?.Content?.Headers);
        }

        public void AddAccount(AcmeAccountKeys keys)
        {
            AddAccount(keys.KeyId, keys.PrivateKey.Jwk());
        }

        public void AddAccount(string location, Jwk jwk)
        {
            _createdAccounts[location] = jwk;
            CreatedAccounts.Add(location);
        }

        public void AddOrder(string location, AcmeOrderResponse order)
        {
            _orders[location] = order;
            _handlers[(location, HttpMethod.Post)] = HandleRetrieveOrderRequest;

            if (!string.IsNullOrWhiteSpace(order.Finalize))
                _handlers[(order.Finalize, HttpMethod.Post)] = HandleFinalizeRequest;

            if (!string.IsNullOrWhiteSpace(order.Certificate))
                _handlers[(order.Certificate, HttpMethod.Post)] = HandleDownloadCertificateRequest;

            foreach (var auth in order.Authorizations)
            {
                _handlers[(auth, HttpMethod.Post)] = HandleAuthorizationsRequest;
            }
        }

        private async Task<Jws> ValidateAndGetJws(HttpContent content, bool ensureJwkPresent = false, bool ensureKeyIdPresent = false)
        {
            var jws = await content.ReadFromJsonAsync<Jws>();

            ValidateJws(jws, ensureJwkPresent: ensureJwkPresent, ensureKidPresent: ensureKeyIdPresent);

            return jws;
        }

        private void ValidateJws(Jws jws, bool ensureJwkPresent = false, bool ensureKidPresent = false)
        {
            var @protected = Deserialize.FromBase64Url<ProtectedHeader>(jws.Protected);

            if (ensureJwkPresent && @protected.Jwk == null)
                throw new ApplicationException("Kid missing from protected header");

            if (ensureKidPresent && string.IsNullOrWhiteSpace(@protected.Kid))
                throw new ApplicationException("Kid missing from protected header");

            var jwk = @protected.Jwk ?? _createdAccounts[@protected.Kid];

            Validate.Signature(jwk, jws);

            Validate.ProtectedHeader(@protected, _issuedNonces, _usedNonces);

            _usedNonces.Add(@protected.Nonce);
        }

        public string Resource(string path)
        {
            return $"{BaseUrl}/{path}";
        }

        public void AddChallenge(AcmeChallenge challenge)
        {
            _challenges[challenge.Url] = challenge;
            _handlers[(challenge.Url, HttpMethod.Post)] = HandleChallengeRequest;
        }
    }
}
