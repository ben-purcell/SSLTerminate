using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SSLTerminate.Extensions
{
    public static class X509Certificate2Extensions
    {
        public static X509Certificate2 CopyWithRsaPrivateKey(
            this X509Certificate2 certificate, 
            byte[] rsaPrivateKey)
        {
            var rsa = RSA.Create();

            rsa.ImportRSAPrivateKey(rsaPrivateKey, out _);

            certificate = certificate.CopyWithPrivateKey(rsa);

            return certificate;
        }

        public static bool HasExpired(this X509Certificate2 certificate) =>
            certificate.NotAfter <= DateTime.Now;
    }
}
