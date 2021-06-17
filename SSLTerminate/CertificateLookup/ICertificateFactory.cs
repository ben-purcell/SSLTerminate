using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTerminate.CertificateLookup
{
    /// <summary>
    /// Responsible for certificate creation - is used by ICertificateLookupService
    /// to create certificate if one cannot be found. The default implementation for
    /// this is to use CertificateFactory which creates a certificate via ACME v2 client
    /// i.e. Let's Encrypt
    /// </summary>
    public interface ICertificateFactory
    {
        Task<X509Certificate2> Create(string host, CancellationToken cancellationToken = default);
    }
}