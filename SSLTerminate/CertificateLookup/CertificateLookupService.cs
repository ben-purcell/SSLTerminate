using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SSLTerminate.Exceptions;
using SSLTerminate.Stores.ClientCertificates;
using SSLTerminate.Whitelist;

namespace SSLTerminate.CertificateLookup
{
    class CertificateLookupService : ICertificateLookupService
    {
        private readonly IWhitelistService _whitelistService;
        private readonly IClientCertificateStore _clientCertificateStore;
        private readonly ICertificateFactory _certificateFactory;
        private static readonly SemaphoreSlim CertCreationSemaphore = new SemaphoreSlim(initialCount:1);

        public CertificateLookupService(
            IWhitelistService whitelistService,
            IClientCertificateStore clientCertificateStore, 
            ICertificateFactory certificateFactory)
        {
            _whitelistService = whitelistService;
            _clientCertificateStore = clientCertificateStore;
            _certificateFactory = certificateFactory;
        }

        public async Task<X509Certificate2> GetForHostAsync(string host, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new HostMissingException();

            var isAllowed = await _whitelistService.IsAllowed(host);
            if (!isAllowed)
                throw new HostNotAllowedException($"Host not allowed: {host}");

            var certificateWithPrivateKey = await _clientCertificateStore.GetCertificateWithPrivateKey(host);

            if (certificateWithPrivateKey == null)
            {
                await CertCreationSemaphore.WaitAsync(cancellationToken);
                try
                {
                    certificateWithPrivateKey = await _clientCertificateStore.GetCertificateWithPrivateKey(host);

                    // one more check after acquiring lock
                    if (certificateWithPrivateKey != null)
                    {
                        return certificateWithPrivateKey;
                    }

                    certificateWithPrivateKey = await _certificateFactory.Create(host, cancellationToken);
                    await _clientCertificateStore.Store(host, certificateWithPrivateKey);
                }
                finally
                {
                    CertCreationSemaphore.Release();
                }
            }

            return certificateWithPrivateKey;
        }
    }
}