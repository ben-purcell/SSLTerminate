using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.ReverseProxy.Models
{
    public class AddRegisteredRouteRequest
    {
        [Required]
        public string Host { get; set; }

        [Required]
        public string Redirect { get; set; }
    }
}