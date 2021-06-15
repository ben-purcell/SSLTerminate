using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SSLTerminate.Stores.ClientCertificates
{
    public interface IClientCertificateStore
    {
        Task<X509Certificate2> GetCertificateWithPrivateKey(string host);

        Task Store(string host, X509Certificate2 certificateWithPrivateKey);
    }
}