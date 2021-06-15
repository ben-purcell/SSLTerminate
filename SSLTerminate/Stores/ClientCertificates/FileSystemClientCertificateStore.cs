using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SSLTerminate.Extensions;

namespace SSLTerminate.Stores.ClientCertificates
{
    public class FileSystemClientCertificateStore : IClientCertificateStore
    {
        private readonly ILogger<FileSystemClientCertificateStore> _logger;
        private readonly FileSystemClientCertificateStoreConfig _config;

        public FileSystemClientCertificateStore(
            IOptions<FileSystemClientCertificateStoreConfig> config,
            ILogger<FileSystemClientCertificateStore> logger)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<X509Certificate2> GetCertificateWithPrivateKey(string host)
        {
            _logger.LogInformation("Getting certificate for host: " + host);

            var path = GetPath(host);

            if (!File.Exists(path))
            {
                _logger.LogInformation("Unable to find certificate for host: " + host);
                return null;
            }

            _logger.LogDebug("Found certificate for host: " + host);

            var json = await File.ReadAllTextAsync(path);

            _logger.LogDebug("Read certificate bytes for host: " + host);

            var deserialized = JsonSerializer.Deserialize<CertificateWithPrivateKeyStored>(json);

            var certificateBytes = WebEncoders.Base64UrlDecode(deserialized.CertificateBase64UrlEncoded);
            var privateKeyBytes = WebEncoders.Base64UrlDecode(deserialized.PrivateKeyBase64UrlEncoded);
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var certificate = new X509Certificate2(certificateBytes)
                .CopyWithPrivateKey(rsa);

            if (certificate.HasExpired())
            {
                _logger.LogInformation("Certificate expired, returning null");
                return null;
            }

            _logger.LogInformation("Loaded certificate for host: " + host);

            return certificate;
        }

        public async Task Store(string host, X509Certificate2 certificate)
        {
            if (!certificate.HasPrivateKey)
                throw new ApplicationException("Cannot store certificate without private key for host: " + host);

            _logger.LogInformation("Storing certificate for host: " + host);

            Directory.CreateDirectory(_config.ClientCertificatePath);

            var path = GetPath(host);

            var privateKey = certificate.PrivateKey.ExportPkcs8PrivateKey();

            var deserialized = new CertificateWithPrivateKeyStored
            {
                PrivateKeyBase64UrlEncoded = WebEncoders.Base64UrlEncode(privateKey),
                CertificateBase64UrlEncoded = WebEncoders.Base64UrlEncode(certificate.RawData),
            };

            var json = JsonSerializer.Serialize(deserialized);

            await File.WriteAllTextAsync(path, json);

            _logger.LogInformation("Stored certificate for host: " + host);
        }

        private string GetPath(string host)
        {
            _logger.LogDebug($"Getting path for host: {host}");

            var path = Path.Combine(_config.ClientCertificatePath, host);

            _logger.LogDebug($"Host and path: {host}   ---   {path}");

            return path;
        }
    }

    public class CertificateWithPrivateKeyStored
    {
        public string CertificateBase64UrlEncoded { get; set; }
        public string PrivateKeyBase64UrlEncoded { get; set; }
    }
}
