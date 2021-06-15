using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTerminate.CertificateLookup
{
    public interface ICertificateFactory
    {
        Task<X509Certificate2> Create(string host, CancellationToken cancellationToken = default);
    }
}