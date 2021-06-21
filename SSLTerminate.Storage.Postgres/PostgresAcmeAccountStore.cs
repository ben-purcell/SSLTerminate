using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;
using SSLTerminate.ACME.Keys;
using SSLTerminate.Stores.AcmeAccounts;

namespace SSLTerminate.Storage.Postgres
{
    public class PostgresAcmeAccountStore : IAcmeAccountStore
    {
        private readonly string _connectionString;

        public PostgresAcmeAccountStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<AcmeAccountKeys> Get()
        {
            const string sql = "select * from AcmeAccountKeys";

            await using var connection = new NpgsqlConnection(_connectionString);
            var keys = await connection.QueryAsync<AcmeAccountKeys>(sql);

            return keys.FirstOrDefault();
        }

        public async Task Store(AcmeAccountKeys keys)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.InsertAsync(keys);
        }
    }
}
