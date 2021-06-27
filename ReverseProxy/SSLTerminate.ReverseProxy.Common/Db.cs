using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace SSLTerminate.ReverseProxy.Common
{
    public class Db
    {
        public static void CreateTables(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);

            var script = File.ReadAllText(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "scripts",
                "create_reverse_proxy_tables.pgsql"));

            connection.Execute(script);
        }

        public static void DropTables(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);

            var script = File.ReadAllText(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "scripts",
                "drop_reverse_proxy_tables.pgsql"));

            connection.Execute(script);
        }
    }
}
