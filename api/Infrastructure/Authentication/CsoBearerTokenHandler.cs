using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Helpers.Extensions;

namespace Scv.Api.Infrastructure.Authentication
{
    public sealed partial class CsoBearerTokenHandler(
        IKeycloakTokenService tokenService,
        IOptions<CsoKeycloakClientOptions> options,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CsoBearerTokenHandler> logger) : DelegatingHandler
    {
        /// <summary>
        /// Url pattern for retrieving CSO documents
        /// </summary>
        /// <returns>Returns true if Url path matches the CSO GetJudicialDocument endpoint pattern.</returns>
        [GeneratedRegex(@"/judicial/[^/]+/document/?$", RegexOptions.IgnoreCase)]
        private static partial Regex GetJudicialDocumentUrlRegex();

        private static readonly object UserAccessTokenItemsKey = new();
        private static readonly Lock UserAccessTokenLock = new();
        private readonly IKeycloakTokenService _tokenService = tokenService;
        private readonly CsoKeycloakClientOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<CsoBearerTokenHandler> _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Headers.Authorization == null)
            {
                string token;
                if (IsGetJudicialDocumentRequest(request))
                {
                    _logger.LogDebug("CSO GetJudicialDocument request detected; using current user's access token.");
                    token = await GetUserAccessTokenAsync(cancellationToken)
                        ?? await _tokenService.GetServiceAccountTokenAsync(_options, cancellationToken);
                }
                else
                {
                    token = await _tokenService.GetServiceAccountTokenAsync(_options, cancellationToken);
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogDebug("CSO request already contains an Authorization header.");
            }

            _logger.LogInformation("Sending request to {RequestUri} with method {Method}", request.RequestUri, request.Method);

            return await base.SendAsync(request, cancellationToken);
        }

        private static bool IsGetJudicialDocumentRequest(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get || request.RequestUri == null)
            {
                return false;
            }

            return GetJudicialDocumentUrlRegex().IsMatch(request.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Retrieves the current user's access token, exchanging it at most once per request.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>User access token</returns>
        private Task<string> GetUserAccessTokenAsync(CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("No HttpContext available; cannot obtain user access token.");
                return Task.FromResult<string>(null);
            }

            // Reuse a single exchange per request so merging multiple documents doesn't trigger
            // repeated Keycloak refreshes or cookie re-issues (which would overflow response headers).
            lock (UserAccessTokenLock)
            {
                if (httpContext.Items.TryGetValue(UserAccessTokenItemsKey, out var cached)
                    && cached is Task<string> cachedTask)
                {
                    return cachedTask;
                }

                var exchangeTask = ExchangeAndPersistUserAccessTokenAsync(httpContext, cancellationToken);
                httpContext.Items[UserAccessTokenItemsKey] = exchangeTask;
                return exchangeTask;
            }
        }

        private async Task<string> ExchangeAndPersistUserAccessTokenAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            var refreshToken = await httpContext.GetTokenAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, "refresh_token");
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("No refresh_token found for the current user; cannot obtain user access token.");
                return null;
            }

            var client = _httpClientFactory.CreateClient(KeycloakTokenService.HttpClientName);
            var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = _configuration.GetNonEmptyValue("Keycloak:Authority") + "/protocol/openid-connect/token",
                ClientId = _configuration.GetNonEmptyValue("Keycloak:Client"),
                ClientSecret = _configuration.GetNonEmptyValue("Keycloak:Secret"),
                RefreshToken = refreshToken
            }, cancellationToken);

            if (response.IsError || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _logger.LogError(
                    "Failed to exchange user refresh_token for access_token. Error: {Error}. Description: {ErrorDescription}",
                    response.Error ?? "unknown", response.ErrorDescription);
                return null;
            }

            try
            {
                // Persist the rotated refresh token and expiry back to the auth cookie. This runs once
                // per request (guarded by the HttpContext.Items cache), so the cookie is re-issued a
                // single time regardless of how many documents are retrieved.
                var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (authResult.Succeeded && authResult.Properties != null)
                {
                    authResult.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);
                    authResult.Properties.UpdateTokenValue(
                        "expires_at",
                        DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn)
                            .ToString("o", System.Globalization.CultureInfo.InvariantCulture));
                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        authResult.Principal,
                        authResult.Properties);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist rotated refresh_token back to the auth cookie.");
            }

            return response.AccessToken;
        }
    }
}
