using System;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeOrderResponse
    {
        public string Status { get; set; }

        public DateTime Expires { get; set; }

        public DateTime? NotBefore { get; set; }

        public DateTime? NotAfter { get; set; }

        public AcmeIdentifier[] Identifiers { get; set; }

        public string[] Authorizations { get; set; }

        public string Finalize { get; set; }

        public string Certificate { get; set; }

        public string Location { get; set; }
    }
}