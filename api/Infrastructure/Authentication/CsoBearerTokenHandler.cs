using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;

namespace Scv.Api.Infrastructure.Authentication
{
    public sealed class CsoBearerTokenHandler : DelegatingHandler
    {
        private readonly IKeycloakTokenService _tokenService;
        private readonly KeycloakClientOptions _options;
        private readonly ILogger<CsoBearerTokenHandler> _logger;

        public CsoBearerTokenHandler(
            IKeycloakTokenService tokenService,
            IOptions<KeycloakClientOptions> options,
            ILogger<CsoBearerTokenHandler> logger)
        {
            _tokenService = tokenService;
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Headers.Authorization == null)
            {
                var token = await _tokenService.GetServiceAccountTokenAsync(_options, cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogDebug("CSO request already contains an Authorization header.");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
