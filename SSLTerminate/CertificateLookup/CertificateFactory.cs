using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SSLTerminate.ACME;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Exceptions;
using SSLTerminate.Extensions;
using SSLTerminate.Stores.AcmeAccounts;
using SSLTerminate.Stores.KeyAuthorizations;
using SSLTerminate.Utils;

namespace SSLTerminate.CertificateLookup
{
    class CertificateFactory : ICertificateFactory
    {
        private readonly IAcmeClientFactory _acmeClientFactory;
        private readonly IAcmeAccountStore _acmeAccountStore;
        private readonly IKeyAuthorizationsStore _keyAuthorizationsStore;
        private readonly CertificateRequestFactory _certificateRequestFactory;
        private readonly ILogger<CertificateFactory> _logger;
        private readonly SslTerminateConfig _config;

        public CertificateFactory(
            IAcmeClientFactory acmeClientFactory,
            IAcmeAccountStore acmeAccountStore,
            IKeyAuthorizationsStore keyAuthorizationsStore,
            CertificateRequestFactory certificateRequestFactory,
            IOptions<SslTerminateConfig> config,
            ILogger<CertificateFactory> logger)
        {
            _acmeClientFactory = acmeClientFactory;
            _acmeAccountStore = acmeAccountStore;
            _keyAuthorizationsStore = keyAuthorizationsStore;
            _certificateRequestFactory = certificateRequestFactory;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<X509Certificate2> Create(string host, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Creating X509 certificate for host: {host}");

            var acmeClient = await _acmeClientFactory.Create();

            var accountKeys = await GetOrCreateAccountKeys(acmeClient, cancellationToken);

            var order = await acmeClient.CreateOrder(accountKeys, AcmeOrderRequest.CreateForHost(host), cancellationToken);

            var authorizations = await acmeClient.GetAuthorizations(accountKeys, order.Authorizations[0], cancellationToken);

            var http01Challenge = authorizations
                .Challenges
                .First(x => x.Type.Equals("http-01", StringComparison.OrdinalIgnoreCase));

            await _keyAuthorizationsStore.Store(
                http01Challenge.Token, 
                accountKeys.PrivateKey.KeyAuthorization(http01Challenge.Token));

            await acmeClient.SignalReadyForChallenge(accountKeys, http01Challenge, cancellationToken);

            order = await WaitForChallengeToComplete(
                acmeClient, accountKeys, order, cancellationToken);

            if (order.Status.Equals(AcmeOrderStatus.Invalid, StringComparison.OrdinalIgnoreCase))
                throw new CertificateCreationException($"Unable to create certificate, order invalid: {order.Location}");

            if (!order.Status.Equals(AcmeOrderStatus.Ready, StringComparison.OrdinalIgnoreCase))
                throw new CertificateCreationException($"Expected order {order.Location} to be ready, status was {order.Status}");

            var (privateKey, csr) = _certificateRequestFactory.Create(host);
            order = await acmeClient.Finalize(accountKeys, order.Finalize, AcmeFinalizeRequest.ForCsr(csr));

            order = await WaitForCertificateToBeAvailable(
                acmeClient, accountKeys, order, cancellationToken);

            if (!order.Status.Equals(AcmeOrderStatus.Valid, StringComparison.OrdinalIgnoreCase))
                throw new CertificateCreationException("Expected order to move to valid state after cert finalized, state: " + order.Status);

            if (string.IsNullOrWhiteSpace(order.Certificate))
                throw new CertificateCreationException($"Unable to download certificate, no url present. Order: {order.Location}, status: {order.Status}");

            var pemChain = await acmeClient.DownloadCertificate(accountKeys, order.Certificate, cancellationToken);

            var certificate = new X509Certificate2(Encoding.ASCII.GetBytes(pemChain))
                .CopyWithRsaPrivateKey(privateKey);

            await _keyAuthorizationsStore.Remove(http01Challenge.Token);

            return certificate;
        }

        private static async Task<AcmeOrderResponse> WaitForChallengeToComplete(
            IAcmeClient acmeClient, 
            AcmeAccountKeys accountKeys, 
            AcmeOrderResponse order,
            CancellationToken cancellationToken)
        {
            order = await acmeClient.PollWhileOrderInStatus(
                accountKeys, 
                order.Location, 
                statuses: new[]
                {
                    AcmeOrderStatus.Pending
                });

            return order;
        }

        private static async Task<AcmeOrderResponse> WaitForCertificateToBeAvailable(
            IAcmeClient acmeClient, AcmeAccountKeys accountKeys, 
            AcmeOrderResponse order,
            CancellationToken cancellationToken)
        {
            if (!order.Status.Equals(AcmeOrderStatus.Valid))
            {
                order = await acmeClient.PollWhileOrderInStatus(
                    accountKeys, 
                    order.Location, 
                    statuses: new[]
                    {
                        AcmeOrderStatus.Ready,
                        AcmeOrderStatus.Processing
                    });
            }

            return order;
        }

        private async Task<AcmeAccountKeys> GetOrCreateAccountKeys(
            IAcmeClient acmeClient, CancellationToken cancellationToken)
        {
            var accountKeys = await _acmeAccountStore.Get();
            if (accountKeys == null)
            {
                var privateKey = new RsaPrivateKey();

                var request = new AcmeAccountRequest
                {
                    Contact = _config
                        .AccountContacts
                        .Select(x => $"mailto:{x}")
                        .ToArray(),
                    TermsOfServiceAgreed = true
                };

                var account = await acmeClient.CreateAccount(request, privateKey, cancellationToken);

                accountKeys = new AcmeAccountKeys
                {
                    KeyId = account.Location,
                    PrivateKey = privateKey
                };

                await _acmeAccountStore.Store(accountKeys);
            }

            return accountKeys;
        }
    }
}