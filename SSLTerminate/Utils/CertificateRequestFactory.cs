using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SSLTerminate.Utils
{
    class CertificateRequestFactory : ICertificateRequestFactory
    {
        public (byte[] privateKey, byte[] csr) Create(string host)
        {
            var rsa = RSA.Create();

            var request = new CertificateRequest($"CN={host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var csr = request.CreateSigningRequest(X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1));

            var privateKey = rsa.ExportRSAPrivateKey();

            return (privateKey, csr);
        }
    }
}