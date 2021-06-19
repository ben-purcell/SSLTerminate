using System;
using System.Threading.Tasks;
using SSLTerminate.ACME;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeAcmeClientFactory : IAcmeClientFactory
    {
        private readonly IAcmeClient _acmeClient;

        public FakeAcmeClientFactory(IAcmeClient acmeClient)
        {
            _acmeClient = acmeClient;
        }

        public Task<IAcmeClient> Create()
        {
            return Task.FromResult(_acmeClient);
        }
    }
}