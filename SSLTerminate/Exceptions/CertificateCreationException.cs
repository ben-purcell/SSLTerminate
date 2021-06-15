using System;

namespace SSLTerminate.Exceptions
{
    public class CertificateCreationException : Exception
    {
        public CertificateCreationException(string message)
        : base(message)
        {
        }
    }
}
