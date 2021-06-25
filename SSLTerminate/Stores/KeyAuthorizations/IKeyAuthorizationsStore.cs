using System.Threading.Tasks;

namespace SSLTerminate.Stores.KeyAuthorizations
{
    public interface IKeyAuthorizationsStore
    {
        Task<string> GetKeyAuthorization(string token);
        
        Task Store(string host, string token, string keyAuthorization);

        Task Remove(string token);
    }
}