using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.Whitelist
{
    public class InMemoryWhitelistServiceConfig
    {
        [Required]
        public string[] AllowedHosts { get; set; }
    }
}
