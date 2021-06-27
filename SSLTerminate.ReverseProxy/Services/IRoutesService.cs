using System.Threading.Tasks;
using SSLTerminate.ReverseProxy.Controllers;
using SSLTerminate.ReverseProxy.Entities;

namespace SSLTerminate.ReverseProxy.Services
{
    public interface IRoutesService
    {
        Task<RegisteredRoute> Get(string host);
        Task Add(RegisteredRoute registeredRoute);
    }
}