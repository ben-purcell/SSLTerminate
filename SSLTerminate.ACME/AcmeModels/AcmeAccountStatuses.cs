using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeAccountStatuses
    {
        public const string Valid = "valid";
        public const string Deactivated = "deactivated";
        public const string Revoked = "revoked";
    }
}
