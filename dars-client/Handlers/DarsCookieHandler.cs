using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DARSCommon.Handlers
{
    /// <summary>
    /// HTTP message handler that forwards the LogSheetSessionService.Token cookie from incoming requests to outgoing DARS requests
    /// </summary>
    public class DarsCookieHandler : DelegatingHandler
    {
        private const string DEFAULT_COOKIE_NAME = "LogSheetSessionService.Token";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DarsCookieHandler> _logger;
        private readonly string _logsheetSessionCookieName;

        public DarsCookieHandler(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<DarsCookieHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _logsheetSessionCookieName = configuration["DARS:LogSheetSessionCookieName"] ?? DEFAULT_COOKIE_NAME;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the LogSheet session cookie from the incoming request
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Cookies != null &&
                httpContext.Request.Cookies.TryGetValue(_logsheetSessionCookieName, out var cookieValue))
            {
                _logger.LogDebug("Found {CookieName} cookie in request, forwarding to DARS API", _logsheetSessionCookieName);

                // Add the cookie to the outgoing request
                request.Headers.Add("Cookie", $"{_logsheetSessionCookieName}={cookieValue}");
            }
            else
            {
                _logger.LogDebug("{CookieName} cookie not found in incoming request", _logsheetSessionCookieName);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
