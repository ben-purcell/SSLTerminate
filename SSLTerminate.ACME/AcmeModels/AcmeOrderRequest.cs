namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeOrderRequest
    {
        public AcmeIdentifier[] Identifiers { get; set; }

        public static AcmeOrderRequest CreateForHost(string host)
        {
            return new AcmeOrderRequest
            {
                Identifiers = new[]
                {
                    new AcmeIdentifier
                    {
                        Type = "dns",
                        Value = host
                    }
                }
            };
        }
    }
}
