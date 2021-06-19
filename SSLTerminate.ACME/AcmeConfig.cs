using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.ACME
{
    public class AcmeConfig
    {
        [Required]
        public string DirectoryUrl { get; set; }
    }
}