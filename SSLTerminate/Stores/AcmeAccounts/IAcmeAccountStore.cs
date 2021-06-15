using System.Threading.Tasks;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.Stores.AcmeAccounts
{
    public interface IAcmeAccountStore
    {
        Task<AcmeAccountKeys> Get();

        Task Store(AcmeAccountKeys account);
    }
}