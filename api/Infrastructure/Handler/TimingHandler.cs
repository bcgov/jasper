using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Infrastructure.Handler
{
    /// <summary>
    /// This is created so we can measure the time between request and responses. There doesn't seem to be a very easy way of doing this in .NET Core. 
    /// I tried enabling scopes, but they seemed to print the same TraceId/SpanId/ConnectionId/RequestId. 
    /// </summary>
    public class TimingHandler : DelegatingHandler
    {
        private readonly ILogger<TimingHandler> _logger;

        public TimingHandler(ILogger<TimingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var guid = Guid.NewGuid();

            _logger.LogInformation($"Starting request {guid} - {request.RequestUri.AbsoluteUri}");
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            _logger.LogInformation($"Finished request {guid} - {request.RequestUri.AbsoluteUri} in {sw.ElapsedMilliseconds}ms");

            return response;
        }
    }
}
