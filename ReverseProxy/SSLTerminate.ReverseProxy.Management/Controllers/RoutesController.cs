using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SSLTerminate.ReverseProxy.Common.Entities;
using SSLTerminate.ReverseProxy.Common.Services;
using SSLTerminate.ReverseProxy.Management.Models;

namespace SSLTerminate.ReverseProxy.Management.Controllers
{
    [Route("routes")]
    public class RoutesController : Controller
    {
        private readonly IRegisteredRoutesService _registeredRoutesService;

        public RoutesController(IRegisteredRoutesService registeredRoutesService)
        {
            _registeredRoutesService = registeredRoutesService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]string host)
        {
            var registered = await _registeredRoutesService.Get(host);

            return registered != null
                ? (IActionResult) Ok(registered)
                : NotFound("Registered Route with given host not found");
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody]AddRegisteredRouteRequest model)
        {
            var registeredRoute = new RegisteredRoute
            {
                Host = model.Host.ToLowerInvariant(),
                Upstream = model.Redirect.ToLowerInvariant(),
                CreatedUtc = DateTime.UtcNow
            };

            await _registeredRoutesService.Add(registeredRoute);

            var location = Url.Action(nameof(Get));

            return Accepted(location);
        }

        [HttpDelete]
        public async Task<IActionResult> Remove([FromQuery]string host)
        {
            await _registeredRoutesService.Remove(host);
            return Ok();
        }
    }
}
