namespace SSLTerminate.ACME.Exceptions
{
    public class AcmeTimeoutException : System.Exception
    {
        public AcmeTimeoutException(string message)
            : base(message)
        {
        }
    }
}