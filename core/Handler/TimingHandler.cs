using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Scv.Core.Infrastructure.Handler
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
            ArgumentNullException.ThrowIfNull(request);

            var sw = Stopwatch.StartNew();
            var guid = Guid.NewGuid();

            _logger.LogInformation("Starting request {Guid} - {AbsoluteUri}", guid, request.RequestUri?.AbsoluteUri);
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            _logger.LogInformation("Finished request {Guid} - {AbsoluteUri} in {ElapsedMilliseconds}ms", guid, request.RequestUri?.AbsoluteUri, sw.ElapsedMilliseconds);

            return response;
        }
    }
}
