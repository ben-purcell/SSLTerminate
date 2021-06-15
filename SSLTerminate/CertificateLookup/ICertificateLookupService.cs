using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SSLTerminate.CertificateLookup
{
    public interface ICertificateLookupService
    {
        X509Certificate2 GetForHost(string host);

        Task<X509Certificate2> GetForHostAsync(string host, CancellationToken cancellationToken = default);
    }
}