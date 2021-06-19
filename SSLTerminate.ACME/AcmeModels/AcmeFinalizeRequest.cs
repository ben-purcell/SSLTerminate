using Microsoft.AspNetCore.WebUtilities;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeFinalizeRequest
    {
        public string Csr { get; set; }

        public static AcmeFinalizeRequest ForCsr(byte[] certificateSigningRequest)
        {
            var encodedCsr = WebEncoders.Base64UrlEncode(certificateSigningRequest);

            return new AcmeFinalizeRequest
            {
                Csr = encodedCsr
            };
        }
    }
}