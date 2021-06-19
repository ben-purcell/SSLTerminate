using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSLTerminate.ACME
{
    public class Directories
    {
        public class LetsEncrypt
        {
            public const string Staging = "https://acme-staging-v02.api.letsencrypt.org/directory";

            public const string Production = "https://acme-v02.api.letsencrypt.org/directory";
        }
    }
}
