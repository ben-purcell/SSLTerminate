using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SSLTerminate.ACME.JWS
{
    public class Jwk
    {
        public string Kty { get; set; }

        public string N { get; set; }

        public string E { get; set; }
    }
}