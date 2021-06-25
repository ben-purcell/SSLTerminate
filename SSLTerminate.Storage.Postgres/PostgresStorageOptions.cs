using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.Storage.Postgres
{
    public class PostgresStorageOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}