using System;

namespace SSLTerminate.ACME.JWS
{
    class JwsRequestException : Exception
    {
        public JwsRequestException(string message)
            : base(message)
        {
        }
    }
}