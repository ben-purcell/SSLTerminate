using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTerminate.CertificateLookup
{
    /// <summary>
    /// The entry-point for certificate lookup - this service should:
    /// * check host is allowed ssl using IWhitelistService
    /// * attempt to lookup certificate for given host using IClientCertificateStore
    /// * if unable to find certificate, use ICertificateFactory to create a new one
    /// * newly created certs are stored in IClientCertificateStore
    /// </summary>
    public interface ICertificateLookupService
    {
        Task<X509Certificate2> GetForHostAsync(string host, CancellationToken cancellationToken = default);
    }
}