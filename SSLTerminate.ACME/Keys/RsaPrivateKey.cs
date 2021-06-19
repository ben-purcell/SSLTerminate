using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SSLTerminate.ACME.JWS;

namespace SSLTerminate.ACME.Keys
{
    public class RsaPrivateKey : IPrivateKey
    {
        private readonly RSA _rsa;

        public byte[] Bytes { get; }

        public string KeyType => "RSA";

        public string JwsAlg => "RS256";

        public Jwk Jwk()
        {
            var securityKey = new RsaSecurityKey(_rsa);

            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(securityKey);

            var acmeJwk = new Jwk
            {
                E = jwk.E,
                N = jwk.N,
                Kty = jwk.Kty,
            };

            return acmeJwk;
        }

        public byte[] Sign(byte[] bytes)
        {
            var signedData = _rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signedData;
        }

        public byte[] Thumbprint()
        {
            var jwk = Jwk();

            // order of fields matter as per boulder source code
            var formatted = $"{{\"e\":\"{jwk.E}\",\"kty\":\"{jwk.Kty}\",\"n\":\"{jwk.N}\"}}";
            var bytes = Encoding.UTF8.GetBytes(formatted);
            var thumbprint = SHA256.HashData(bytes);
            return thumbprint;
        }

        public RsaPrivateKey(byte[] bytes)
        {
            Bytes = bytes;
            _rsa = RSA.Create();
            _rsa.ImportRSAPrivateKey(Bytes, out _);
        }

        public RsaPrivateKey()
        {
            _rsa = RSA.Create();
            Bytes = _rsa.ExportRSAPrivateKey();
        }

        public RSAParameters RsaParameters => _rsa.ExportParameters(includePrivateParameters: true);
    }
}