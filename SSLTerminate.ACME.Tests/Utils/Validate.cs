using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using SSLTerminate.ACME.JWS;

namespace SSLTerminate.ACME.Tests.Utils
{
    class Validate
    {
        public static void Signature(
            Jwk jwk, 
            Jws jws)
        {
            var data = Encoding.ASCII.GetBytes($"{jws.Protected}.{jws.Payload}");

            var rsa = RSA.Create();

            var rsaParameters = new RSAParameters
            {
                Modulus = WebEncoders.Base64UrlDecode(jwk.N),
                Exponent = WebEncoders.Base64UrlDecode(jwk.E),
            };

            rsa.ImportParameters(rsaParameters);

            var signature = WebEncoders.Base64UrlDecode(jws.Signature);

            var isValidSignature = rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!isValidSignature)
                throw new ApplicationException("Invalid signature");
        }

        public static void ProtectedHeader(
            ProtectedHeader @protected,
            IEnumerable<string> issuedNonces,
            IEnumerable<string> usedNonces)
        {
            if (!issuedNonces.Contains(@protected.Nonce))
                throw new ApplicationException("Previously issued nonce not found in protected header");

            if (usedNonces.Contains(@protected.Nonce))
                throw new ApplicationException("Nonce previously used");
        }

        public static void JoseJsonContentType(HttpContentHeaders headers)
        {
            if (!headers?.ContentType.ToString().Equals("application/jose+json") ?? false)
                throw new ApplicationException("Expected Content-Type header to be 'application/jose+json'");
        }

        public static void IsEmptyJsonMessage(Jws jws)
        {
            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(jws.Payload));

            if (decoded != "{}")
                throw new ApplicationException("Expected Jws payload to be empty json: {}, got: " + decoded);
        }

        public static void IsPostAsGet(Jws jws)
        {
            if (jws?.Payload != string.Empty)
                throw new ApplicationException("Expected empty string to indicate POST-as-GET request");
        }
    }
}
