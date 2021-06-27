using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SSLTerminate.ReverseProxy.Entities;
using SSLTerminate.ReverseProxy.Models;
using SSLTerminate.ReverseProxy.Services;

namespace SSLTerminate.ReverseProxy.Controllers
{
    [Route("routes")]
    public class RoutesController : Controller
    {
        private readonly IRoutesService _routesService;

        public RoutesController(IRoutesService routesService)
        {
            _routesService = routesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistered([FromQuery]string host)
        {
            var registered = await _routesService.Get(host);
            return Ok(registered);
        }

        [HttpPost]
        public async Task<IActionResult> AddRegistered([FromBody]AddRegisteredRouteRequest model)
        {
            var registeredRoute = new RegisteredRoute
            {
                Host = model.Host,
                Redirect = model.Redirect
            };

            await _routesService.Add(registeredRoute);

            var location = Url.Action(nameof(GetRegistered));

            return Accepted(location);
        }
    }
}
