using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SSLTerminate.Whitelist;

namespace SSLTerminate.Storage.Postgres
{
    class PostgresWhitelistService : IWhitelistService
    {
        private readonly ILogger<PostgresWhitelistService> _logger;
        private readonly string _connectionString;

        public PostgresWhitelistService(
            IOptions<PostgresStorageOptions> options,
            ILogger<PostgresWhitelistService> logger)
        {
            _logger = logger;
            _connectionString = options.Value.ConnectionString;
        }

        public async Task<bool> IsAllowed(string host)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql = 
                "select * " +
                "from WhitelistEntry h " +
                "where h.Host = @Host ";

            var whitelisted = (await connection.QueryAsync<WhitelistEntry>(sql, new
                {
                    Host = host
                }))
                .FirstOrDefault();

            var result = whitelisted != null;

            _logger.LogDebug($"Checking if host allowed: '{host}' - result = '{result}'");

            return result;
        }

        public async Task Add(string host)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql =
                "insert into WhitelistEntry (Host, CreatedUtc)" +
                " values (@Host, @CreatedUtc) " +
                "on conflict (Host) do nothing";

            var rowsAffected = await connection.ExecuteAsync(sql, new WhitelistEntry
            {
                Host = host,
                CreatedUtc = DateTime.UtcNow
            });

            _logger.LogDebug($"Added host: '{host}', rows affected: {rowsAffected}");
        }

        public async Task Remove(string host)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql =
                "delete from WhitelistEntry where Host = @Host";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Host = host
            });

            _logger.LogDebug($"Removed host: '{host}', rows affected: {rowsAffected}");
        }
    }

    class WhitelistEntry
    {
        public string Host { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
