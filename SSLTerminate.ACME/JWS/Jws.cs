using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using SSLTerminate.ACME.Common;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME.JWS
{
    class Jws
    {
        public string Protected { get; set; }

        public string Payload { get; set; }

        public string Signature { get; set; }

        public static Jws Create(IPrivateKey privateKey, string nonce, string url, object payload)
        {
            var @protected = new ProtectedHeader
            {
                Alg = privateKey.JwsAlg,
                Jwk = privateKey.Jwk(),
                Nonce = nonce,
                Url = url,
            };

            return Create(privateKey, payload, @protected);
        }

        public static Jws Create(IPrivateKey privateKey, string keyId, string nonce, string url, object payload = null)
        {
            var @protected = new ProtectedHeader
            {
                Alg = privateKey.JwsAlg,
                Kid = keyId,
                Nonce = nonce,
                Url = url,
            };

            return Create(privateKey, payload, @protected);
        }

        public static Jws Create(IPrivateKey privateKey, object payload, object @protected)
        {
            var payloadJson = payload != null
                ? Json.Serialize(payload)
                : string.Empty;

            var protectedJson = Json.Serialize(@protected);

            var base64UrlPayload = Base64Url(payloadJson);
            var base64UrlProtected = Base64Url(protectedJson);
            var base64UrlSignature = Base64UrlSignature(privateKey, base64UrlProtected, base64UrlPayload);

            return new Jws
            {
                Protected = base64UrlProtected,
                Payload = base64UrlPayload,
                Signature = base64UrlSignature
            };
        }

        private static string Base64UrlSignature(IPrivateKey privateKey, string base64UrlProtected, string base64UrlPayload)
        {
            var signature = $"{base64UrlProtected}.{base64UrlPayload}";
            var byesToSign = Encoding.ASCII.GetBytes(signature);

            var signatureBytes = privateKey.Sign(byesToSign);
            var base64UrlSignature = Base64Url(signatureBytes);
            return base64UrlSignature;
        }

        private static string Base64Url(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string Base64Url(byte[] bytes)
        {
            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}