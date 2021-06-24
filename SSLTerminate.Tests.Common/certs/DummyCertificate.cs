using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SSLTerminate.Tests.Common.certs
{
    public class DummyCertificate
    {
        private static readonly Lazy<X509Certificate2> CertificateOne = new Lazy<X509Certificate2>(
            () => X509Certificate2.CreateFromPemFile(
                certPemFilePath: Path.Join("certs", "one.pem"),
                keyPemFilePath: Path.Join("certs", "one_key.pem")));

        private static readonly Lazy<X509Certificate2> CertificateTwo = new Lazy<X509Certificate2>(
            () => X509Certificate2.CreateFromPemFile(
                certPemFilePath: Path.Join("certs", "two.pem"),
                keyPemFilePath: Path.Join("certs", "two_key.pem")));
        public static X509Certificate2 One()
        {
            return CertificateOne.Value;
        }

        public static X509Certificate2 Two()
        {
            return CertificateTwo.Value;
        }

        public static (byte[] privateKey, string pem) CreateDummyPrivateKeyAndPem()
        {
            var privateKeyStr = File.ReadAllText(Path.Join("certs", "one_key.pem"));
            var pem = File.ReadAllText(Path.Join("certs", "one.pem"));

            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyStr);

            var privateKeyBytes = rsa.ExportRSAPrivateKey();

            return (privateKeyBytes, pem);
        }
    }
}