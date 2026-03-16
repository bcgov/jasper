using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Exceptions;
using Scv.Api.Infrastructure.Options;

namespace Scv.Api.Infrastructure.Authentication
{
    public interface IKeycloakTokenService
    {
        Task<string> GetServiceAccountTokenAsync(KeycloakClientOptions options, CancellationToken cancellationToken = default);
    }

    public sealed class KeycloakTokenService : IKeycloakTokenService
    {
        public const string HttpClientName = "KeycloakTokenClient";

        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KeycloakTokenService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public KeycloakTokenService(
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<KeycloakTokenService> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GetServiceAccountTokenAsync(KeycloakClientOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidateOptions(options);
            ValidateSecret(options);

            var cacheKey = BuildCacheKey(options);
            if (_cache.TryGetValue(cacheKey, out ServiceAccountToken cachedToken) &&
                cachedToken.IsValid(options.RefreshSkewSeconds, options.ClockSkewSeconds))
            {
                return cachedToken.AccessToken;
            }

            var tokenLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(cacheKey, out cachedToken) &&
                    cachedToken.IsValid(options.RefreshSkewSeconds, options.ClockSkewSeconds))
                {
                    return cachedToken.AccessToken;
                }

                var token = await RequestServiceAccountTokenAsync(options, cancellationToken);
                var cacheDuration = GetCacheDuration(token.ExpiresAt, options.RefreshSkewSeconds, options.ClockSkewSeconds);

                if (cacheDuration.TotalSeconds > options.RefreshSkewSeconds)
                {
                    _cache.Set(cacheKey, token, cacheDuration);
                }
                else
                {
                    _logger.LogWarning("Unable to set valid cache duration, below skew threshold.");
                }


                return token.AccessToken;
            }
            finally
            {
                tokenLock.Release();
            }
        }

        private async Task<ServiceAccountToken> RequestServiceAccountTokenAsync(
            KeycloakClientOptions options,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var tokenEndpoint = options.Authority.TrimEnd('/') + "/protocol/openid-connect/token";

            _logger.LogInformation(
                "Requesting Keycloak service account token for authority {Authority}, clientId {ClientId}, audience {Audience}, scope {Scope}.",
                options.Authority,
                options.ClientId,
                string.IsNullOrWhiteSpace(options.Audience) ? "(null)" : options.Audience,
                string.IsNullOrWhiteSpace(options.Scope) ? "(null)" : options.Scope);

            var request = new ClientCredentialsTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = options.ClientId,
                ClientSecret = options.ServiceAccountSecret,
                Scope = string.IsNullOrWhiteSpace(options.Scope) ? null : options.Scope
            };

            if (!string.IsNullOrWhiteSpace(options.Audience))
            {
                _logger.LogDebug("Adding audience parameter to token request: {Audience}", options.Audience);
                request.Parameters.Add("audience", options.Audience);
            }
            else
            {
                _logger.LogWarning("No audience configured in KeycloakClientOptions. Token may not include audience claim.");
            }

            var response = await client.RequestClientCredentialsTokenAsync(request, cancellationToken);
            if (response.IsError || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _logger.LogError(
                    "Failed to retrieve Keycloak service account token. Error: {Error}. Description: {ErrorDescription}",
                    response.Error ?? "unknown", response.ErrorDescription);
                throw new InvalidOperationException("Unable to retrieve service account token from Keycloak.");
            }

            if (response.ExpiresIn <= 0)
            {
                _logger.LogWarning("Keycloak token response did not include a valid expires_in value. Falling back to JWT exp claim or default expiry.");
            }

            var expiresAt = ComputeExpiry(response);

            return new ServiceAccountToken(response.AccessToken, expiresAt);
        }

        private static string BuildCacheKey(KeycloakClientOptions options)
        {
            return $"keycloak-token::{options.Authority}::{options.ClientId}::{options.Audience}::{options.Scope}";
        }

        private static TimeSpan GetCacheDuration(DateTimeOffset expiresAt, int refreshSkewSeconds, int clockSkewSeconds)
        {
            var skew = TimeSpan.FromSeconds(Math.Max(0, refreshSkewSeconds) + Math.Max(0, clockSkewSeconds));
            var duration = expiresAt - DateTimeOffset.UtcNow - skew;

            return duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(1);
        }

        private static DateTimeOffset ComputeExpiry(TokenResponse response)
        {
            if (response.ExpiresIn > 0)
            {
                return DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(response.AccessToken);
            var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out var expUnix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(expUnix);
            }

            return DateTimeOffset.UtcNow.AddSeconds(300);
        }

        private static void ValidateOptions(KeycloakClientOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Authority))
            {
                throw new ConfigurationException("Configuration 'Authority' is invalid or missing.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                throw new ConfigurationException("Configuration 'ClientId' is invalid or missing.");
            }
        }

        private static void ValidateSecret(KeycloakClientOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ServiceAccountSecret))
            {
                throw new ConfigurationException("Configuration 'ServiceAccountSecret' is invalid or missing.");
            }
        }

        private sealed record ServiceAccountToken(string AccessToken, DateTimeOffset ExpiresAt)
        {
            public bool IsValid(int refreshSkewSeconds, int clockSkewSeconds)
            {
                var skew = TimeSpan.FromSeconds(Math.Max(0, refreshSkewSeconds) + Math.Max(0, clockSkewSeconds));
                return ExpiresAt - skew > DateTimeOffset.UtcNow;
            }
        }
    }
}
