using SSLTerminate.ACME.JWS;

namespace SSLTerminate.ACME.Keys
{
    public interface IPrivateKey
    {
        public byte[] Bytes { get; }

        public string KeyType { get; }

        public string JwsAlg { get; }

        public Jwk Jwk();

        public byte[] Sign(byte[] bytes);

        public byte[] Thumbprint();
    }
}