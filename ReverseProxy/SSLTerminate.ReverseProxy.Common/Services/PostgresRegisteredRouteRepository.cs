using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SSLTerminate.ReverseProxy.Common.Entities;
using SSLTerminate.Storage.Postgres;

namespace SSLTerminate.ReverseProxy.Common.Services
{
    class PostgresRegisteredRouteRepository : IRegisteredRouteRepository
    {
        private readonly ILogger<PostgresRegisteredRouteRepository> _logger;
        private readonly string _connectionString;

        public PostgresRegisteredRouteRepository(
            IOptions<PostgresStorageOptions> options,
            ILogger<PostgresRegisteredRouteRepository> logger)
        {
            _logger = logger;
            _connectionString = options.Value.ConnectionString;
        }

        public async Task Add(RegisteredRoute registeredRoute)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql = 
                "insert into RegisteredRoute(Host, Upstream, CreatedUtc) " +
                "values (@Host, @Upstream, @CreatedUtc)" +
                "on conflict(Host) do update set Upstream = @Upstream";

            var affectedRows = await connection.ExecuteAsync(sql, registeredRoute);

            _logger.LogDebug($"Registered Route: {registeredRoute.Host} -> {registeredRoute.Upstream}, affected rows: {affectedRows}");
        }

        public async Task RemoveByHost(string host)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql = "delete from RegisteredRoute where Host = @Host";

            var affectedRows = await connection.ExecuteAsync(sql, new { Host = host });

            _logger.LogDebug($"Deleted route by host: {host}, affected rows: {affectedRows}");
        }

        public async Task<RegisteredRoute> GetByHost(string host)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            const string sql = "select * from RegisteredRoute where Host = @Host";

            var registeredRoute = (await connection.QueryAsync<RegisteredRoute>(sql, new
            {
                Host = host
            })).FirstOrDefault();

            var logMessage = registeredRoute != null
                ? $"Get RegisteredRoute by host ({host}): {registeredRoute?.Host} -> {registeredRoute?.Upstream}"
                : $"Get RegisteredRoute by host ({host}): not found";

            _logger.LogDebug(logMessage);

            return registeredRoute;
        }
    }
}