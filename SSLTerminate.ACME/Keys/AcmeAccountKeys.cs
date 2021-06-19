namespace SSLTerminate.ACME.Keys
{
    public class AcmeAccountKeys
    {
        /// <summary>
        /// Location (url) of the ACME Account
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Private key ACME Account was created with
        /// </summary>
        public IPrivateKey PrivateKey { get; set; }
    }
}