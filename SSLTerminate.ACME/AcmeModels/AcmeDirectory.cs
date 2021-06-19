using System.Text.Json;
using System.Text.Json.Serialization;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeDirectory
    {
        public string NewNonce { get; set; }

        public string NewAccount { get; set; }

        public string NewOrder { get; set; }

        public string RevokeCert { get; set; }

        public string KeyChange { get; set; }
    }
}