using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Npgsql;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Stores.AcmeAccounts;

namespace SSLTerminate.Storage.Postgres
{
    public class PostgresAcmeAccountStore : IAcmeAccountStore
    {
        private readonly string _connectionString;

        public PostgresAcmeAccountStore(IOptions<PostgresStorageOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
        }

        public async Task<AcmeAccountKeys> Get()
        {
            const string sql = "select * from AccountKeys";

            await using var connection = new NpgsqlConnection(_connectionString);
            var keys = (await connection.QueryAsync<AccountKeys>(sql)).FirstOrDefault();

            if (keys == null)
                return null;

            var decodedPrivateKey = WebEncoders.Base64UrlDecode(keys.PrivateKeyBase64Url);

            var acmeAccountKeys = new AcmeAccountKeys
            {
                KeyId = keys.KeyId,
                PrivateKey = new RsaPrivateKey(decodedPrivateKey)
            };

            return acmeAccountKeys;
        }

        public async Task Store(AcmeAccountKeys acmeAccountKeys)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var encodedPrivateKey = WebEncoders.Base64UrlEncode(acmeAccountKeys.PrivateKey.Bytes);

            var keys = new AccountKeys
            {
                KeyId = acmeAccountKeys.KeyId,
                PrivateKeyBase64Url = encodedPrivateKey,
                CreatedUtc = DateTime.UtcNow
            };

            var sql = "insert into AccountKeys (KeyId, PrivateKeyBase64Url, CreatedUtc) values(@KeyId, @PrivateKeyBase64Url, @CreatedUtc)";

            await connection.ExecuteAsync(sql, keys);
        }

        class AccountKeys
        {
            public string KeyId { get; set; }
            public string PrivateKeyBase64Url { get; set; }
            public DateTime CreatedUtc { get; set; }
        }
    }
}
