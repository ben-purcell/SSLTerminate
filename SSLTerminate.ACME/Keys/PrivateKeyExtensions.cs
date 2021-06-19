using Microsoft.AspNetCore.WebUtilities;

namespace SSLTerminate.ACME.Keys
{
    public static class PrivateKeyExtensions
    {
        public static string KeyAuthorization(this IPrivateKey privateKey, string token)
        {
            // think I saw this idea in the source code of certes. cheers

            var thumbprint = privateKey.Thumbprint();

            var base64UrlThumbprint = WebEncoders.Base64UrlEncode(thumbprint);

            var keyAuthorization = $"{token}.{base64UrlThumbprint}";

            return keyAuthorization;
        }
    }
}