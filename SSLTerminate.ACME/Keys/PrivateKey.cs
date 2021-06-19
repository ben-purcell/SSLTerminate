using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSLTerminate.ACME.Keys
{
    public class PrivateKey
    {
        public static IPrivateKey Create(string keyType, byte[] bytes)
        {
            if (!keyType.Equals("RSA"))
                throw new NotSupportedException("Only RSA keyType currently supported");

            return new RsaPrivateKey(bytes);
        }
    }
}
