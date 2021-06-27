using System;
using System.Threading.Tasks;
using System.Transactions;
using SSLTerminate.ReverseProxy.Common.Entities;
using SSLTerminate.Whitelist;

namespace SSLTerminate.ReverseProxy.Common.Services
{
    class RegisteredRoutesService : IRegisteredRoutesService
    {
        private readonly IWhitelistService _whitelistService;
        private readonly IRegisteredRouteRepository _registeredRouteRepository;

        public RegisteredRoutesService(
            IWhitelistService whitelistService,
            IRegisteredRouteRepository registeredRouteRepository)
        {
            _whitelistService = whitelistService;
            _registeredRouteRepository = registeredRouteRepository;
        }

        public async Task<RegisteredRoute> Get(string host)
        {
            var route = await _registeredRouteRepository.GetByHost(host);
            return route;
        }

        public async Task Add(RegisteredRoute registeredRoute)
        {
            using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await _whitelistService.Add(registeredRoute.Host);
            await _registeredRouteRepository.Add(registeredRoute);

            tx.Complete();
        }

        public async Task Remove(string host)
        {
            using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await _whitelistService.Remove(host);
            await _registeredRouteRepository.RemoveByHost(host);

            tx.Complete();
        }
    }
}