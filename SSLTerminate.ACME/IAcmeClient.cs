using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME
{
    public interface IAcmeClient
    {
        Task<AcmeAccountResponse> CreateAccount(
            AcmeAccountRequest acmeAccountRequest,
            IPrivateKey privateKey,
            CancellationToken cancellationToken = default);

        Task<AcmeOrderResponse> CreateOrder(
            AcmeAccountKeys keys, 
            AcmeOrderRequest orderRequest,
            CancellationToken cancellationToken = default);

        Task<AcmeAuthorizationsResponse> GetAuthorizations(
            AcmeAccountKeys keys, 
            string authorizationUrl,
            CancellationToken cancellationToken = default);

        Task<AcmeChallengeResponse> SignalReadyForChallenge(
            AcmeAccountKeys keys, 
            AcmeChallenge challenge,
            CancellationToken cancellationToken = default);

        Task<AcmeOrderResponse> GetOrder(
            AcmeAccountKeys keys, 
            string orderLocation,
            CancellationToken cancellationToken = default);

        Task<AcmeOrderResponse> Finalize(
            AcmeAccountKeys accountKeys, 
            string orderFinalizeUrl, 
            AcmeFinalizeRequest request,
            CancellationToken cancellationToken = default);

        Task<string> DownloadCertificate(
            AcmeAccountKeys accountKeys, 
            string orderCertificate,
            CancellationToken cancellationToken = default);
    }
}
