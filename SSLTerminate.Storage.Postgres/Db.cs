using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace SSLTerminate.Storage.Postgres
{
    public class Db
    {
        public static void CreateStores(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);

            var script = File.ReadAllText(Path.Combine("scripts", "create_tables.pgsql"));

            connection.Execute(script);
        }

        public static void DropStores(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);

            var script = File.ReadAllText(Path.Combine("scripts", "drop_tables.pgsql"));

            connection.Execute(script);
        }
    }
}
