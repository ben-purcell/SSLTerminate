using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SSLTerminate.ACME.AcmeModels;
using SSLTerminate.ACME.Exceptions;
using SSLTerminate.ACME.Keys;

namespace SSLTerminate.ACME
{
    public static class AcmeClientExtensions
    {
        public static async Task<AcmeOrderResponse> PollWhileOrderInStatus(
            this IAcmeClient client,
            AcmeAccountKeys keys,
            string orderLocation,
            string[] statuses,
            int timeoutSeconds = 180,
            int pollFrequencySeconds = 10,
            CancellationToken cancellationToken = default)
        {
            var sw = new Stopwatch();
            sw.Start();

            while (sw.Elapsed.Seconds < timeoutSeconds)
            {
                var orderResponse = await client.GetOrder(keys, orderLocation, cancellationToken);

                if (!statuses.Any(status => orderResponse.Status.Equals(status, StringComparison.OrdinalIgnoreCase)))
                    return orderResponse;

                await Task.Delay(TimeSpan.FromSeconds(pollFrequencySeconds), cancellationToken);
            }

            sw.Stop();

            throw new AcmeTimeoutException($"Polling location '{orderLocation}' for status change from '{AcmeOrderStatus.Pending}', timed out after: {timeoutSeconds}");
        }
    }
}
