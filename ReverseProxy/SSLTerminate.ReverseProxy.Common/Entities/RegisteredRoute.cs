using System;

namespace SSLTerminate.ReverseProxy.Common.Entities
{
    public class RegisteredRoute
    {
        public string Host { get; set; }
        public string Upstream { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}