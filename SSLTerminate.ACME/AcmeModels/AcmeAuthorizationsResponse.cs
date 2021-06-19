using System;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeAuthorizationsResponse
    {
        public string Status { get; set; }

        public DateTime Expires { get; set; }

        public AcmeIdentifier Identifier { get; set; }

        public AcmeChallenge[] Challenges { get; set; }
    }
}