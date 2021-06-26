using System.Threading.Tasks;

namespace SSLTerminate.Whitelist
{
    public interface IWhitelistService
    {
        Task<bool> IsAllowed(string host);

        Task Add(string host);

        Task Remove(string host);
    }
}