namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeAccountRequest
    {
        public bool TermsOfServiceAgreed { get; set; }

        public string[] Contact { get; set; }

        public static AcmeAccountRequest CreateForEmail(string email)
        {
            return new AcmeAccountRequest
            {
                TermsOfServiceAgreed = true,
                Contact = new []
                {
                    $"mailto:{email}"
                }
            };
        }
    }
}