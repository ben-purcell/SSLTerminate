using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.Whitelist
{
    public class FixedHostsWhitelistServiceConfig
    {
        [Required]
        public string[] AllowedHosts { get; set; }
    }
}
