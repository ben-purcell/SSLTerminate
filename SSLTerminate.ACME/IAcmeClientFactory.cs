using System.IO;
using System.Threading.Tasks;

namespace SSLTerminate.ACME
{
    public interface IAcmeClientFactory
    {
        Task<IAcmeClient> Create();
    }
}