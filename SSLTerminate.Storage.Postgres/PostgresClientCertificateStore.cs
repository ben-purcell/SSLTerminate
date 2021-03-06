using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using SSLTerminate.Extensions;
using SSLTerminate.Stores.ClientCertificates;

namespace SSLTerminate.Storage.Postgres
{
    public class PostgresClientCertificateStore : IClientCertificateStore
    {
        private readonly string _connectionString;

        public PostgresClientCertificateStore(IOptions<PostgresStorageOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
        }

        public async Task<X509Certificate2> GetCertificateWithPrivateKey(string host)
        {
            const string sql = "select * " +
                               "from CertificateWithKey c " +
                               "where c.host = @host " +
                               "and c.expiry > @tomorrow";

            await using var connection = new NpgsqlConnection(_connectionString);
            
            var cert = (await connection.QueryAsync<CertificateWithKey>(
                sql, new
                {
                    host = host,
                    tomorrow = DateTime.Now.AddDays(1)
                }))
                .FirstOrDefault();

            if (cert == null)
                return null;

            var certificateBytes = WebEncoders.Base64UrlDecode(cert.CertificateBase64Url);
            var privateKeyBytes = WebEncoders.Base64UrlDecode(cert.PrivateKeyBase64Url);
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var certificate = new X509Certificate2(certificateBytes)
                .CopyWithPrivateKey(rsa);

            return certificate.HasExpired()
                ? null
                : certificate;
        }

        public async Task Store(string host, X509Certificate2 certificateWithPrivateKey)
        {
            var privateKey = certificateWithPrivateKey.PrivateKey?.ExportPkcs8PrivateKey()
                ?? throw new ApplicationException("Unable to store certificate without private key");

            var cert = new CertificateWithKey
            {
                Host = host,
                Expiry = certificateWithPrivateKey.NotAfter,
                PrivateKeyBase64Url = WebEncoders.Base64UrlEncode(privateKey),
                CertificateBase64Url = WebEncoders.Base64UrlEncode(certificateWithPrivateKey.RawData),
                CreatedUtc = DateTime.UtcNow,
            };

            await using var connection = new NpgsqlConnection(_connectionString);

            var sql = "insert into CertificateWithKey (Host, Expiry, PrivateKeyBase64Url, CertificateBase64Url, CreatedUtc)  " +
                      "values (@Host, @Expiry, @PrivateKeyBase64Url, @CertificateBase64Url, @CreatedUtc)";

            await connection.ExecuteAsync(sql, cert);
        }
    }

    [Table(nameof(CertificateWithKey))]
    class CertificateWithKey
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public string PrivateKeyBase64Url { get; set; }
        public string CertificateBase64Url { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime Expiry { get; set; }
    }
}
