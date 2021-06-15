using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SSLTerminate.Whitelist
{
    class FixedHostsWhitelistService : IWhitelistService
    {
        private readonly ILogger<FixedHostsWhitelistService> _logger;
        private readonly InMemoryWhitelistServiceConfig _config;

        public FixedHostsWhitelistService(IOptions<InMemoryWhitelistServiceConfig> config, ILogger<FixedHostsWhitelistService> logger)
        {
            _logger = logger;
            _config = config.Value;
        }

        public Task<bool> IsAllowed(string host)
        {
            _logger.LogDebug($"Checking if host allowed: {host}");

            var result = _config.AllowedHosts.Any(x => x.Equals(host));

            _logger.LogDebug($"Host allowed check result: {host} - {result}");

            return Task.FromResult(result);
        }
    }
}
