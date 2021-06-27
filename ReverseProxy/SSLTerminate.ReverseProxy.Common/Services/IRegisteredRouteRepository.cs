using System.Threading.Tasks;
using SSLTerminate.ReverseProxy.Common.Entities;

namespace SSLTerminate.ReverseProxy.Common.Services
{
    public interface IRegisteredRouteRepository
    {
        Task Add(RegisteredRoute registeredRoute);

        Task RemoveByHost(string host);

        Task<RegisteredRoute> GetByHost(string host);
    }
}