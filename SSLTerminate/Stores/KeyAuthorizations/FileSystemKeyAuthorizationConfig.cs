using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.Stores.KeyAuthorizations
{
    public class FileSystemKeyAuthorizationConfig
    {
        [Required]
        public string KeyAuthorizationsPath { get; set; }
    }
}