using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SSLTerminate.CertificateLookup;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeCertificateFactory : ICertificateFactory
    {
        private readonly Dictionary<string, X509Certificate2> _certificateDict = 
            new Dictionary<string, X509Certificate2>();

        public Task<X509Certificate2> Create(string host, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_certificateDict[host]);
        }

        public FakeCertificateFactory ThatCreates(string host, X509Certificate2 certificate)
        {
            _certificateDict[host] = certificate;
            return this;
        }
    }
}