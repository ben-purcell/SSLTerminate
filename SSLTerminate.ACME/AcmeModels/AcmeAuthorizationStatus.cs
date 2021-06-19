namespace SSLTerminate.ACME.AcmeModels
{
    public class AcmeAuthorizationStatus
    {
        public const string Pending = "pending";
        public const string Revoked = "revoked";
        public const string Valid = "valid";
        public const string Invalid = "invalid";
        public const string Deactivated = "deactivated";
        public const string Expired = "expired";
    }
}