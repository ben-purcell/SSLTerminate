using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SSLTerminate.Stores.KeyAuthorizations
{
    class FileSystemKeyAuthorizationsStore : IKeyAuthorizationsStore
    {
        private readonly ILogger<FileSystemKeyAuthorizationsStore> _logger;
        private readonly FileSystemKeyAuthorizationConfig _config;

        public FileSystemKeyAuthorizationsStore(IOptions<FileSystemKeyAuthorizationConfig> config, ILogger<FileSystemKeyAuthorizationsStore> logger)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<string> GetKeyAuthorization(string token)
        {
            _logger.LogDebug("Retrieving key authorization for token: " + token);

            var path = GetPath(token);
            if (!File.Exists(path))
            {
                _logger.LogDebug("Key authorization not found: " + token);
                return null;
            }

            var contents = await File.ReadAllTextAsync(path);

            _logger.LogDebug("Retrieved key authorization for token: " + token);

            return contents;
        }

        public async Task Store(string host, string token, string keyAuthorization)
        {
            _logger.LogDebug("Storing key authorization for token: " + token);

            Directory.CreateDirectory(_config.KeyAuthorizationsPath);

            var path = GetPath(token);

            await File.WriteAllTextAsync(path, keyAuthorization);

            _logger.LogDebug("Stored key authorization for token: " + token);
        }

        public Task Remove(string token)
        {
            _logger.LogDebug("Removing key authorization for token: " + token);

            var path = GetPath(token);

            if (File.Exists(path))
                File.Delete(path);

            _logger.LogDebug("Removed key authorization for token: " + token);

            return Task.CompletedTask;
        }

        private string GetPath(string token)
        {
            var path = Path.Combine(_config.KeyAuthorizationsPath, token);

            _logger.LogDebug($"Token: {token} - path: {path}");

            return path;
        }
    }
}