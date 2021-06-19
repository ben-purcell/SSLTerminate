using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Keys;
using SSLTerminate.ACME.Tests.Fakes;

namespace SSLTerminate.ACME.Tests.Unit
{
    public class AcmeClientTests
    {
        private AcmeClient _client;
        private FakeAcmeServer _acmeServer;

        [SetUp]
        public void SetUp()
        {
            _acmeServer = new FakeAcmeServer();

            _client = new AcmeClient(
                _acmeServer,
                FakeAcmeServer.Directory,
                NullLogger<AcmeClient>.Instance);
        }

        [Test]
        public async Task account_can_be_created()
        {
            var acmeAccountResponse = await _client.CreateAccount(
                AcmeAccountRequest.CreateForEmail("test@test.com"),
                new RsaPrivateKey());

            acmeAccountResponse.Should().BeEquivalentTo(new AcmeAccountResponse
            {
                Status = AcmeAccountStatuses.Valid,
                Contact = new [] { "mailto:test@test.com" },
                CreatedAt = _acmeServer.Now,
                Location = _acmeServer.CreatedAccounts.Last()
            });
        }

        [Test]
        public async Task order_can_be_placed()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-location")
            };

            _acmeServer.AddAccount(keys);

            var orderResponse = await _client.CreateOrder(keys, AcmeOrderRequest.CreateForHost("test.com"));

            orderResponse.Location.Should().NotBeNull();
            orderResponse.Status.Should().Be(AcmeOrderStatus.Pending);
            orderResponse.Identifiers.Should().BeEquivalentTo(new[]
            {
                new AcmeIdentifier {Type = "dns", Value = "test.com"}
            });
        }

        [Test]
        public async Task authorizations_can_be_retrieved()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-account")
            };

            _acmeServer.AddAccount(keys);

            _acmeServer.AddOrder(_acmeServer.Resource("test-order"), new AcmeOrderResponse
            {
                Authorizations = new []
                {
                    _acmeServer.Resource("auth1")
                },
                Identifiers = new [] 
                { 
                    new AcmeIdentifier
                    {
                        Type = "dns", Value = "www.somehost.com"
                    }
                }
            });

            var authz = await _client.GetAuthorizations(
                keys, 
                authorizationUrl: _acmeServer.Resource("auth1"));

            authz.Status.Should().Be(AcmeAuthorizationStatus.Pending);
            authz.Identifier.Should().BeEquivalentTo(new AcmeIdentifier { Type = "dns", Value = "www.somehost.com" });
            authz.Expires.Should().Be(_acmeServer.OrderExpiry);
            authz.Challenges.Should().BeEquivalentTo(new[]
            {
                new {Type = "http-01"},
                new {Type = "dns-01"},
            });
        }

        [Test]
        public async Task existing_order_can_be_retrieved()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-account")
            };

            _acmeServer.AddAccount(keys);

            var orderLocation = _acmeServer.Resource("test-order");

            var acmeOrderResponse = new AcmeOrderResponse
            {
                Status = AcmeOrderStatus.Pending,
                Finalize = _acmeServer.Resource("finalize"),
                Expires = _acmeServer.OrderExpiry,
                Identifiers = new []
                {
                    new AcmeIdentifier { Type = "dns", Value = "www.test.com" }
                },
                Authorizations = new [] { _acmeServer.Resource("auth1") },
                Location = orderLocation
            };

            _acmeServer.AddOrder(orderLocation, acmeOrderResponse);

            var order = await _client.GetOrder(keys, orderLocation);

            order.Should().BeEquivalentTo(acmeOrderResponse);
        }

        [Test]
        public async Task can_signal_ready_for_challenge()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-account")
            };

            _acmeServer.AddAccount(keys);

            var challenge = new AcmeChallenge
            {
                Token = "test",
                Type = "http-01",
                Url = _acmeServer.Resource("test-challenge")
            };

            _acmeServer.AddChallenge(challenge);

            var challengeResponse = await _client.SignalReadyForChallenge(keys, challenge);

            challengeResponse.Should().BeEquivalentTo(new AcmeChallengeResponse
            {
                Token = "test",
                Type = "http-01",
                Url = challenge.Url,
                Status = "pending"
            });
        }

        [Test]
        public async Task order_can_be_finalized()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-account")
            };

            _acmeServer.AddAccount(keys);

            var orderLocation = _acmeServer.Resource("test-order");

            var order = new AcmeOrderResponse
            {
                Status = AcmeOrderStatus.Pending,
                Finalize = _acmeServer.Resource("finalize"),
                Expires = _acmeServer.OrderExpiry,
                Identifiers = new[]
                {
                    new AcmeIdentifier { Type = "dns", Value = "www.test.com" }
                },
                Authorizations = new[] { _acmeServer.Resource("auth1") },
                Location = orderLocation
            };

            _acmeServer.AddOrder(orderLocation, order);

            var response = await _client.Finalize(keys, order.Finalize, new AcmeFinalizeRequest
            {
                Csr = WebEncoders.Base64UrlEncode(new byte[] { 1, 2, 3, 4, 5 })
            });

            response.Should().BeEquivalentTo(order);
        }

        [Test]
        public async Task certificate_can_be_downloaded()
        {
            var keys = new AcmeAccountKeys
            {
                PrivateKey = new RsaPrivateKey(),
                KeyId = _acmeServer.Resource("test-account")
            };

            _acmeServer.AddAccount(keys);

            var orderLocation = _acmeServer.Resource("test-order");

            var order = new AcmeOrderResponse
            {
                Status = AcmeOrderStatus.Pending,
                Finalize = _acmeServer.Resource("finalize"),
                Expires = _acmeServer.OrderExpiry,
                Identifiers = new[]
                {
                    new AcmeIdentifier { Type = "dns", Value = "www.test.com" }
                },
                Authorizations = new[] { _acmeServer.Resource("auth1") },
                Location = orderLocation,
                Certificate = _acmeServer.Resource("test-certificate")
            };

            _acmeServer.AddOrder(orderLocation, order);

            var pemChain = await _client.DownloadCertificate(keys, order.Certificate);

            AssertPemChainValid(pemChain);
        }

        private void AssertPemChainValid(string pemChain)
        {
            pemChain.Should().NotBeNullOrWhiteSpace();

            var certificate = new X509Certificate2(Encoding.ASCII.GetBytes(pemChain));
        }
    }
}