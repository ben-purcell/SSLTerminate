using System.ComponentModel.DataAnnotations;

namespace SSLTerminate
{
    public class SslTerminateConfig
    {
        public string DirectoryUrl { get; set; }

        public string[] AllowHosts { get; set; }

        [Required]
        public string[] AccountContacts { get; set; }
    }
}