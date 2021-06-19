using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using SSLTerminate.ACME.Common;

namespace SSLTerminate.ACME.Tests.Utils
{
    class Deserialize
    {
        public static T FromBase64Url<T>(string base64)
        {
            var value = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(base64));

            var deserialized = Json.Deserialize<T>(value);

            return deserialized;
        }
    }
}
