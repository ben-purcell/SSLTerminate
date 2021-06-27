using System.Threading.Tasks;
using SSLTerminate.ReverseProxy.Common.Entities;

namespace SSLTerminate.ReverseProxy.Common.Services
{
    public interface IRegisteredRoutesService
    {
        Task<RegisteredRoute> Get(string host);
        Task Add(RegisteredRoute registeredRoute);
        Task Remove(string host);
    }
}