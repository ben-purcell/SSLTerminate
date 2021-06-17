using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SSLTerminate.Stores.ClientCertificates;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeClientCertificateStore : IClientCertificateStore
    {
        private readonly Dictionary<string, X509Certificate2> _hostCertificateDict =
            new Dictionary<string, X509Certificate2>();

        public X509Certificate2 GetCertificate(string host)
        {
            if (_hostCertificateDict.TryGetValue(host, out var cert))
                return cert;

            return null;
        }

        public Task<X509Certificate2> GetCertificateWithPrivateKey(string host)
        {
            return Task.FromResult(GetCertificate(host));
        }

        public Task Store(string host, X509Certificate2 certificateWithPrivateKey)
        {
            _hostCertificateDict[host] = certificateWithPrivateKey;
            return Task.CompletedTask;
        }

        public IClientCertificateStore WithCertificate(string host, X509Certificate2 certificate)
        {
            Store(host, certificate);
            return this;
        }
    }
}