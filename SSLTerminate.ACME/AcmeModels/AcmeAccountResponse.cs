using System;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeAccountResponse
    {
        public string[] Contact { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }

        public string Location { get; set; }
    }
}