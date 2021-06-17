using System.Security.Cryptography.X509Certificates;
using SSLTerminate.Utils;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeCertificateRequestFactory : ICertificateRequestFactory
    {
        private readonly byte[] _privateKey;
        private readonly byte[] _csr;

        public FakeCertificateRequestFactory(
            byte[] privateKey, byte[] csr)
        {
            _privateKey = privateKey;
            _csr = csr;
        }

        public (byte[] privateKey, byte[] csr) Create(string host)
        {
            return (_privateKey, _csr);
        }
    }
}