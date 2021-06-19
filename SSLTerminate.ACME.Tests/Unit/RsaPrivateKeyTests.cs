using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using NUnit.Framework;
using SSLTerminate.ACME.JWS;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME.Tests.Unit
{
    public class RsaPrivateKeyTests
    {
        // if one of these changes it will affect the rest
        public class SignatureTesting
        {
            public const string Base64UrlEncodedKey = @"MIIEowIBAAKCAQEAmBUZHXPHEXI3xMqI8OOc-Zz8E8U1JsgkPac92alyRA2j38MNy2UcaTFTU9hicfjM3VjS7BNuFNoHnPMDqVFmI8GDEAJQUzZQ_Sf91JHZBvq4fyaUcBvnvHUMC-_ao-YmsuaJJjYPraji7JFZ1XNeAH1bmLYOoqXGAQPikgIOOwKC1EZzszXFOxrcPajMpbecsXmvpZRGvV0ThlvlPecjb3iZqIRKt88GEZ-m4ltDXtBaOMI7s-xAvIRq1oKWjlBxVlWN9KEWOpK1-rYBit_Eis1xh0lt8TpNZkpMc8gvs0h2akyWxU0kahB1VN6E9CateLNCFekZsx9iyI_50qhkWQIDAQABAoIBABFj6DlL8el2zBW-qyYQgPSyFMkV9dv3at158EKhc6WAgcqmfT9S0orczxB5X9h0gMiWzZedKStNOy2hRDvqSOmxyRDdkt3RCjXIiufxvYVkyhAQE002g6szHyuGQ9QWDzrJzckGyYgsFbbDcChORuN7civmNYo3toqLLS9Q-NuaYsZ-KYj1weYrkY66Y06XUAhxFNE-DPcdGWMBexpjHNzHggF-VGNofcXQiwMHhZujPn7dquxEm-7GI73IS6MkEOKKgSGTEjxY2OMzPpPYyLPlBQeosgLlj80qe5AbZra-2g1vEQy6bg7E6qqTI0uZKV_HwQGTj6CHcgzu-Xp29TkCgYEAx5-VW6X8zhE9AltzP7oDH7qlHl12t8Ro5EP_fzfh-kP8KCoaYouwId_PNol_oSLiJc2NpEWqzmnxx8nrY-o-TEejJZM2cLqusyplDw64TEGs1ojZ0mEiDzn3MLK3w-6U6f0TTfwSnG5Fm8e8WQePbs5UmE82AkSruPUXVZtz6v8CgYEAwwhjNOnJtYJcZDljmQxo50fSRRelX8p-PrI92u0B1rWv_8XdyK9negNGz7z_r4Rj6vg3Du1H7iRRGWJmpCRR9LvGkt-q4ACjra8KCYpRiXq89hyBuV1qNGDaog3BjNS0EV7FDaB1ZbavrV1iMW7dLC_DRgzHIwsB9PdxCzHt6KcCgYEAhOYCepx8PPiPBHW7uY5uK_6HlTqmIdv59RYsEBc1M_d09YxqOndDEJo_CtDpjm553q8FgHr9JySzWc5dDwzQ4tnCjO6ADPbL_e3Yj_i9y87hcYZ0dbJDCZ4OqnYhD6lTrJ_W7VFHVqu3XenQw_jbjeqBuVDq5QGwzZcmLNEskCMCgYBlqEmNmw54faqK0x8G92D2rIj9WoXomDOVmnKDWmZK9Aj42LnxkPvurSaLwYfEhM1P_HE2ZpfHmUZsZM37YLMXTYkDpXH7sFmgfkxNDLvTXRaBcfpsFDT3eER9k43_Sh9RroQnxitrCP4o7zPvcEn4bizqpl5l9abfeNqDa1MGoQKBgFdCe3FRE2BgArrBq8LDKqrcBDlzRKlmZSJVlHh35fmCYDSl2QRDp7Y0zlz63RSgrpHd0xLX_zKc0EM3LHGniNl9CIiviu80-XVsyY3CE-p1L1EYXPRHKiK0AWJzUmJN7CeBD4taZuZm_iHWzTkqwdJLJB2jxgPt_wLpmPnQMW9-";
            public const string Data = "1234567890";
            public const string ExpectedBase64UrlSignature = @"ArYX0heTTa_6butGP_3h9QjcifEj8HKVo8TJMgT50J8cCgFihm5DNFZb-JNb2VvSIA8Ap2NWsceVSyD_IkSIDxnT9r81xG6ajmU3ZtBWr6miJn3LyZT7WPTDN9IPI5sIdp_1zqWPWJMJxi1wlVJzb2uQmEO5kwIy-ug77MAQ48_JDz39bXVQfguLbjxw9kMbURuBiaiFgAYSIK1A13tSHb7uyGovXUF_4bM7hHp1IFxIBuqHl9pfhLp2YUlltr7MhV8mh_mkiwLnAyX3YGtiuRTr0HEPN2-iDQxvMeAupWxWqReyi-ygzuVyq30j9haqLTpLYGOGjNZ5BOTJDgScrw";
            public const string ExpectedBase64UrlThumbprint = @"bY1i3D67oyvEsXh9EyyFwzqNwhfeffJoikV380QLfUw";
        }

        [Test]
        public void creates_jwk_correctly()
        {
            var key = new RsaPrivateKey();

            var rsaParams = key.RsaParameters;

            key.Jwk().Should().BeEquivalentTo(new Jwk
            {
                Kty = "RSA",
                E = WebEncoders.Base64UrlEncode(rsaParams.Exponent),
                N = WebEncoders.Base64UrlEncode(rsaParams.Modulus),
            });
        }

        [Test]
        public void key_type_is_correct()
        {
            new RsaPrivateKey().KeyType.Should().Be("RSA");
        }

        [Test]
        public void jws_alg_is_correct()
        {
            new RsaPrivateKey().JwsAlg.Should().Be("RS256");
        }

        [Test]
        public void can_be_recreated()
        {
            var key = new RsaPrivateKey();

            var key2 = new RsaPrivateKey(key.Bytes);

            key.Jwk().Should().BeEquivalentTo(key2.Jwk());

            key.Bytes.Should().BeEquivalentTo(key2.Bytes);
        }

        [Test]
        public void signature_is_correct()
        {
            var privateKeyBytes = WebEncoders.Base64UrlDecode(SignatureTesting.Base64UrlEncodedKey);
            
            var key = new RsaPrivateKey(privateKeyBytes);

            var signed = key.Sign(Encoding.ASCII.GetBytes(SignatureTesting.Data));

            var base64UrlSignature = WebEncoders.Base64UrlEncode(signed);

            base64UrlSignature.Should().Be(SignatureTesting.ExpectedBase64UrlSignature);
        }

        [Test]
        public void thumbprint_works()
        {
            var privateKeyBytes = WebEncoders.Base64UrlDecode(SignatureTesting.Base64UrlEncodedKey);

            var key = new RsaPrivateKey(privateKeyBytes);

            var thumbprint = key.Thumbprint();

            var base64UrlThumbprint = WebEncoders.Base64UrlEncode(thumbprint);

            base64UrlThumbprint.Should().Be(SignatureTesting.ExpectedBase64UrlThumbprint);
        }
    }
}
