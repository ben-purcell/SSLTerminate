using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Stores.AcmeAccounts;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeAcmeAccountStore : IAcmeAccountStore
    {
        public Task<AcmeAccountKeys> Get()
        {
            return Task.FromResult(Keys);
        }

        public Task Store(AcmeAccountKeys keys)
        {
            Keys = keys;
            return Task.CompletedTask;
        }

        public AcmeAccountKeys Keys { get; private set; }
    }
}