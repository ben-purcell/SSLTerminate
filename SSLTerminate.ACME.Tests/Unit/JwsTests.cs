using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using SSLTerminate.ACME.Common;
using SSLTerminate.ACME.JWS;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME.Tests.Unit
{
    public class JwsTests
    {
        [Test]
        public void jws_with_jwk_can_be_created()
        {
            var payload = new { A = 1, B = 2, C = 3 };

            var privateKey = new RsaPrivateKey();

            var nonce = "test-nonce";

            var url = "https://blah.com";

            var jws = Jws.Create(
                privateKey: privateKey,
                nonce: nonce,
                url: url,
                payload: payload);

            var expectedProtectedHeader = GetProtectedHeaderWithJwk(
                privateKey,
                url,
                nonce);

            jws.Payload.Should().Be(Base64UrlEncoded(payload));
            jws.Protected.Should().Be(Base64UrlEncoded(expectedProtectedHeader));
            jws.Signature.Should().Be(RsaSigned(
                privateKey.Bytes,
                Base64UrlEncoded(expectedProtectedHeader),
                Base64UrlEncoded(payload)));
        }

        [Test]
        public void jws_with_key_id_can_be_created()
        {
            var payload = new { A = 1, B = 2, C = 3 };

            var privateKey = new RsaPrivateKey();

            var nonce = "test-nonce";

            var url = "https://blah.com";

            var jws = Jws.Create(
                privateKey: privateKey,
                keyId: "the-key",
                nonce: nonce,
                url: url,
                payload: payload);

            var expectedProtectedHeader = GetProtectedHeaderWithKeyId(
                privateKey,
                keyId: "the-key",
                url,
                nonce);

            jws.Payload.Should().Be(Base64UrlEncoded(payload));
            jws.Protected.Should().Be(Base64UrlEncoded(expectedProtectedHeader));
            jws.Signature.Should().Be(RsaSigned(
                privateKey.Bytes,
                Base64UrlEncoded(expectedProtectedHeader),
                Base64UrlEncoded(payload)));
        }

        [Test(Description = "Test serialize/deserialize to ensure we don't lose properties")]
        public void protected_header_with_jwk_serializes_and_deserializes_with_correct_fields_hydrated()
        {
            var rsaPrivateKey = new RsaPrivateKey();

            var rsaParams = GetRsaParameters(rsaPrivateKey);

            var jws = Jws.Create(
                rsaPrivateKey,
                nonce: "testing123",
                url: "http://site.com/path",
                payload: new {A = 1}
            );

            var @protected = RehydrateFromBase64(jws.Protected);

            @protected.Should().BeEquivalentTo(new ProtectedHeader
            {
                Alg = "RS256",
                Jwk = new Jwk
                {
                    Kty = "RSA",
                    E = WebEncoders.Base64UrlEncode(rsaParams.Exponent),
                    N = WebEncoders.Base64UrlEncode(rsaParams.Modulus)
                },
                Url = "http://site.com/path",
                Nonce = "testing123"
            });
        }

        [Test(Description = "Test serialize/deserialize to ensure we don't lose properties")]
        public void protected_header_with_key_id_serializes_and_deserializes_with_correct_fields_hydrated()
        {
            var rsaPrivateKey = new RsaPrivateKey();

            var jws = Jws.Create(
                rsaPrivateKey,
                keyId: "1234",
                nonce: "testing123",
                url: "http://site.com/path",
                payload: new { A = 1 }
            );

            var @protected = RehydrateFromBase64(jws.Protected);

            @protected.Should().BeEquivalentTo(new ProtectedHeader
            {
                Alg = "RS256",
                Kid = "1234",
                Url = "http://site.com/path",
                Nonce = "testing123"
            });
        }

        private static RSAParameters GetRsaParameters(RsaPrivateKey rsaPrivateKey)
        {
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(rsaPrivateKey.Bytes, out _);
            var rsaParams = rsa.ExportParameters(includePrivateParameters: false);
            return rsaParams;
        }

        private ProtectedHeader RehydrateFromBase64(string encodedProtectedHeader)
        {
            var bytes = WebEncoders.Base64UrlDecode(encodedProtectedHeader);

            var json = Encoding.UTF8.GetString(bytes);

            var obj = Json.Deserialize<ProtectedHeader>(json);

            return obj;
        }

        private object GetProtectedHeaderWithJwk(RsaPrivateKey privateKey, string url, string nonce)
        {
            var header = new ProtectedHeader
            {
                Alg = privateKey.JwsAlg,
                Jwk = privateKey.Jwk(),
                Url = url,
                Nonce = nonce
            };

            return header;
        }

        private object GetProtectedHeaderWithKeyId(RsaPrivateKey privateKey, string keyId, string url, string nonce)
        {
            var header = new ProtectedHeader
            {
                Alg = privateKey.JwsAlg,
                Kid = keyId,
                Url = url,
                Nonce = nonce
            };

            return header;
        }

        private string RsaSigned(byte[] rsaPrivateKey, string @protected, string payload)
        {
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(rsaPrivateKey, out _);
            var input = Encoding.ASCII.GetBytes($"{@protected}.{payload}");
            var bytes = rsa.SignData(input, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var encoded = WebEncoders.Base64UrlEncode(bytes);

            return encoded;
        }

        private string Base64UrlEncoded(object obj)
        {
            var serialized = Json.Serialize(obj);

            var bytes = Encoding.UTF8.GetBytes(serialized);

            var base64Url = WebEncoders.Base64UrlEncode(bytes);

            return base64Url;
        }
    }
}
