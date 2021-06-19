namespace SSLTerminate.ACME.JWS
{
    class ProtectedHeader
    {
        public string Alg { get; set; }

        public string Nonce { get; set; }

        public string Url { get; set; }

        /// <summary>
        /// Either include Jwk or Kid in requests, not both
        /// </summary>
        public Jwk Jwk { get; set; }

        /// <summary>
        /// Either include Jwk or Kid in requests, not both
        /// </summary>
        public string Kid { get; set; }
    }
}