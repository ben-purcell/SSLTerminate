using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SSLTerminate.Tests.Certs
{
    class DummyCertificate
    {
        private static readonly Lazy<X509Certificate2> CertificateOne = new Lazy<X509Certificate2>(
            () => X509Certificate2.CreateFromPemFile(
                certPemFilePath: @"certs\one.pem", 
                keyPemFilePath: @"certs\one_key.pem"));

        private static readonly Lazy<X509Certificate2> CertificateTwo = new Lazy<X509Certificate2>(
            () => X509Certificate2.CreateFromPemFile(
                certPemFilePath: @"certs\two.pem",
                keyPemFilePath: @"certs\two_key.pem"));
        public static X509Certificate2 One()
        {
            return CertificateOne.Value;
        }

        public static X509Certificate2 Two()
        {
            return CertificateTwo.Value;
        }
    }
}
