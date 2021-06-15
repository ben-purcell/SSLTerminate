using System;

namespace SSLTerminate.Exceptions
{
    internal class HostMissingException : Exception
    {
        public HostMissingException()
            : base("Hostname cannot be found - unsupported browser detected")
        {
        }
    }
}