using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using SSLTerminate.ACME;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Utils;

namespace SSLTerminate.Tests.Fakes
{
    public class HappyPathAcmeClient : IAcmeClient
    {
        private readonly string _pemChain;

        private readonly List<AcmeOrderResponse> _orders = new List<AcmeOrderResponse>();

        private readonly Dictionary<string, int> _orderRequestCount = new Dictionary<string, int>();

        public HappyPathAcmeClient(string pemChain)
        {
            _pemChain = pemChain;
        }

        public Task<AcmeAccountResponse> CreateAccount(
            AcmeAccountRequest acmeAccountRequest, 
            IPrivateKey privateKey,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new AcmeAccountResponse
            {
                Location = "http://acme.com/account-01",
                Contact = acmeAccountRequest.Contact,
                CreatedAt = DateTime.UtcNow,
                Status = AcmeAccountStatuses.Valid
            });
        }

        public Task<AcmeOrderResponse> CreateOrder(
            AcmeAccountKeys keys, 
            AcmeOrderRequest orderRequest,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var order = new AcmeOrderResponse
            {
                Location = $"http://acme.com/order/{Guid.NewGuid()}",
                Status = AcmeOrderStatus.Pending,
                Identifiers = orderRequest.Identifiers,
                Expires = DateTime.UtcNow.AddDays(30),
                Authorizations = Enumerable.Range(0, orderRequest.Identifiers.Length)
                    .Select(x => $"http://acme.com/auth/{Guid.NewGuid()}")
                    .ToArray()
            };

            _orderRequestCount[order.Location] = 0;
            _orders.Add(order);

            return Task.FromResult(order);
        }

        public Task<AcmeAuthorizationsResponse> GetAuthorizations(
            AcmeAccountKeys keys, 
            string authorizationUrl,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new AcmeAuthorizationsResponse
            {
                Challenges = new []
                {
                    new AcmeChallenge
                    {
                        Token = "12345",
                        Type = "http-01",
                        Url = $"http://acme.com/challenge/{Guid.NewGuid()}"
                    },
                    new AcmeChallenge
                    {
                        Token = "54321",
                        Type = "dns-01",
                        Url = $"http://acme.com/challenge/{Guid.NewGuid()}"
                    },
                },
                Identifier = new AcmeIdentifier { Type = "dns", Value = "host.com" },
                Status = AcmeAuthorizationStatus.Pending,
                Expires = DateTime.UtcNow.AddDays(30)
            });
        }

        public Task<AcmeChallengeResponse> SignalReadyForChallenge(
            AcmeAccountKeys keys, 
            AcmeChallenge challenge,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new AcmeChallengeResponse
            {
                Status = "pending",
                Type = challenge.Type,
                Token = challenge.Token,
                Url = challenge.Url
            });
        }

        public Task<AcmeOrderResponse> GetOrder(
            AcmeAccountKeys keys, 
            string orderLocation,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _orderRequestCount[orderLocation] += 1;

            var currentCount = _orderRequestCount[orderLocation];

            var order = GetOrderOrThrow(orderLocation);

            if (currentCount >= 4 && order.Status == AcmeOrderStatus.Processing)
            {
                order.Certificate = $"http://acme.com/certs/{Guid.NewGuid()}";
                order.Status = AcmeOrderStatus.Valid;
            }
            else if (currentCount >= 2 && order.Status == AcmeOrderStatus.Pending)
            {
                order.Status = AcmeOrderStatus.Ready;
            }

            return Task.FromResult(order);
        }

        public Task<AcmeOrderResponse> Finalize(
            AcmeAccountKeys accountKeys, 
            string finalizeUrl, 
            AcmeFinalizeRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var order = GetOrderByFinalizeUrlOrThrow(finalizeUrl);

            order.Status = AcmeOrderStatus.Processing;

            return Task.FromResult(order);
        }

        public Task<string> DownloadCertificate(
            AcmeAccountKeys accountKeys, 
            string orderCertificate,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(_pemChain);
        }

        private AcmeOrderResponse GetOrderOrThrow(string orderLocation)
        {
            var order = _orders.FirstOrDefault(x => x.Location == orderLocation);

            if (order == null)
                throw new ApplicationException("Order not found");
            return order;
        }

        private AcmeOrderResponse GetOrderByFinalizeUrlOrThrow(string finalizeUrl)
        {
            var order = _orders.FirstOrDefault(x => x.Finalize == finalizeUrl);

            if (order == null)
                throw new ApplicationException("Order not found");
            return order;
        }
    }
}