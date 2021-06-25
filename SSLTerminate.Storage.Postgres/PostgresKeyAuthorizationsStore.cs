using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Storage.Postgres
{
    class PostgresKeyAuthorizationsStore : IKeyAuthorizationsStore
    {
        private readonly string _connectionString;

        public PostgresKeyAuthorizationsStore(IOptions<PostgresStorageOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
        }

        public async Task<string> GetKeyAuthorization(string token)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql = @"select * from KeyAuthorization where token = @token";

            var keyAuthorizations = await connection.QueryAsync<KeyAuthorization>(sql, new
            {
                token
            });

            return keyAuthorizations.FirstOrDefault()?.KeyAuth;
        }

        public async Task Store(string token, string keyAuth)
        {
            var keyAuthorization = new KeyAuthorization
            {
                Token = token,
                KeyAuth = keyAuth
            };

            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteAsync(
                "insert into KeyAuthorization values(@Token, @KeyAuth)", keyAuthorization);
        }

        public async Task Remove(string token)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteAsync("delete from KeyAuthorization where Token = @token", new
            {
                token
            });
        }
    }

    [Table(nameof(KeyAuthorization))]
    class KeyAuthorization
    {
        public string Token { get; set; }
        public string KeyAuth { get; set; }
    }
}
