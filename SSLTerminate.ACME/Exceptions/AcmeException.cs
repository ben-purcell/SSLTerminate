using System;

namespace SSLTerminate.ACME.Exceptions
{
    public class AcmeException : Exception
    {
        public AcmeException(string message) : base(message)
        {
        }
    }
}