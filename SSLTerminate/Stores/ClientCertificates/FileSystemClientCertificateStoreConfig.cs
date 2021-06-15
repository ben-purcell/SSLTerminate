using System.ComponentModel.DataAnnotations;

namespace SSLTerminate.Stores.ClientCertificates
{
    public class FileSystemClientCertificateStoreConfig
    {
        [Required]
        public string ClientCertificatePath { get; set; }
    }
}