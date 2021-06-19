namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeChallenge
    {
        public string Type { get; set; }

        public string Url { get; set; }

        public string Token { get; set; }
    }
}