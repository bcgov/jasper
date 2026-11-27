using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Infrastructure.Authentication;

internal sealed class PcssUserListResponseHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (request.Method != HttpMethod.Get ||
            !request.RequestUri.AbsolutePath.EndsWith("/api/auth/users", StringComparison.Ordinal))
        {
            return response;
        }

        try
        {
            // The generated client reads the body without cancellation after receiving headers.
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            return response;
        }
        catch (OperationCanceledException ex) when (response.StatusCode != HttpStatusCode.OK)
        {
            // Preserve an upstream failure even when its error body stalls.
            using (response)
            {
                var status = (int)response.StatusCode;
                var headers = response.Headers.ToDictionary(header => header.Key, header => header.Value);
                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key] = header.Value;
                }
                throw new ApiException($"The HTTP status code of the response was not expected ({status}).", status, null, headers, ex);
            }
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
