using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.Stores.AcmeAccounts
{
    class FileSystemAcmeAccountStore : IAcmeAccountStore
    {
        private readonly FileSystemAcmeAccountStoreConfig _config;

        public FileSystemAcmeAccountStore(IOptions<FileSystemAcmeAccountStoreConfig> config)
        {
            _config = config.Value;
        }

        public async Task<AcmeAccountKeys> Get()
        {
            var path = _config.AcmeAccountPath;
            if (!File.Exists(path))
                return null;

            var serialized = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(serialized))
                return null;

            var serializableKeys = JsonSerializer.Deserialize<SerializableAccountKeys>(serialized);

            var privateKeyBytes = WebEncoders.Base64UrlDecode(serializableKeys.Base64UrlPrivateKey);

            var keys = new AcmeAccountKeys
            {
                KeyId = serializableKeys.KeyId,
                PrivateKey = PrivateKey.Create(serializableKeys.PrivateKeyType, privateKeyBytes)
            };

            return keys;
        }

        public async Task Store(AcmeAccountKeys keys)
        {
            var serializable = new SerializableAccountKeys
            {
                KeyId = keys.KeyId,
                Base64UrlPrivateKey = WebEncoders.Base64UrlEncode(keys.PrivateKey.Bytes),
                PrivateKeyType = keys.PrivateKey.KeyType
            };

            var serialized = JsonSerializer.Serialize(serializable);

            var directory = Path.GetDirectoryName(_config.AcmeAccountPath);

            Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(_config.AcmeAccountPath, serialized);
        }

        class SerializableAccountKeys
        {
            public string KeyId { get; set; }

            public string Base64UrlPrivateKey { get; set; }

            public string PrivateKeyType { get; set; }
        }
    }
}
