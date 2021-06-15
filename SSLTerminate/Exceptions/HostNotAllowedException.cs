using System;

namespace SSLTerminate.Exceptions
{
    public class HostNotAllowedException : Exception
    {
        public HostNotAllowedException(string message)
            :base(message)
        {
        }
    }
}