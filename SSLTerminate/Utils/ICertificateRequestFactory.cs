namespace SSLTerminate.Utils
{
    interface ICertificateRequestFactory
    {
        (byte[] privateKey, byte[] csr) Create(string host);
    }
}