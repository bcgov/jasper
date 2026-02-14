using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Exceptions;
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

            ValidateSecret(options);

            var cacheKey = BuildCacheKey(options);
            if (_cache.TryGetValue(cacheKey, out ServiceAccountToken cachedToken) &&
                cachedToken.IsValid(options.RefreshSkewSeconds))
            {
                return cachedToken.AccessToken;
            }

            var tokenLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(cacheKey, out cachedToken) &&
                    cachedToken.IsValid(options.RefreshSkewSeconds))
                {
                    return cachedToken.AccessToken;
                }

                var token = await RequestServiceAccountTokenAsync(options, cancellationToken);
                var cacheDuration = GetCacheDuration(token.ExpiresAt, options.RefreshSkewSeconds);

                if (cacheDuration.Seconds <= options.RefreshSkewSeconds)
                {
                    _cache.Set(cacheKey, token, cacheDuration);
                }
                else
                {
                    _logger.LogWarning("Unable to set valid cache duration, below skew thereshold.");
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
                "Requesting Keycloak service account token for authority {Authority}, clientId {ClientId}.",
                options.Authority,
                options.ClientId);

            var request = new ClientCredentialsTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = options.ClientId,
                ClientSecret = options.ServiceAccountSecret
            };

            if (!string.IsNullOrWhiteSpace(options.Audience))
            {
                request.Parameters.Add("audience", options.Audience);
            }

            var response = await client.RequestClientCredentialsTokenAsync(request, cancellationToken);
            if (response.IsError || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                _logger.LogError(
                    "Failed to retrieve Keycloak service account token. Error: {Error}.",
                    response.Error ?? "unknown");
                throw new InvalidOperationException("Unable to retrieve service account token from Keycloak.");
            }

            if (response.ExpiresIn <= 0)
            {
                _logger.LogWarning(
                    "Keycloak token response did not include a valid expiry. Using default expiry of 300 seconds.");
            }

            var expiresInSeconds = response.ExpiresIn > 0 ? response.ExpiresIn : 300;
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

            return new ServiceAccountToken(response.AccessToken, expiresAt);
        }

        private static string BuildCacheKey(KeycloakClientOptions options)
        {
            return $"keycloak-token::{options.Authority}::{options.ClientId}::{options.Audience}";
        }

        private static TimeSpan GetCacheDuration(DateTimeOffset expiresAt, int refreshSkewSeconds)
        {
            var skew = TimeSpan.FromSeconds(Math.Max(0, refreshSkewSeconds));
            var duration = expiresAt - DateTimeOffset.UtcNow - skew;

            return duration > TimeSpan.Zero ? duration : TimeSpan.FromSeconds(1);
        }

        private static void ValidateSecret(KeycloakClientOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ServiceAccountSecret))
            {
                throw new ConfigurationException("Configuration 'CsoClientKeycloak:ServiceAccountSecret' is invalid or missing.");
            }
        }

        private sealed record ServiceAccountToken(string AccessToken, DateTimeOffset ExpiresAt)
        {
            public bool IsValid(int refreshSkewSeconds)
            {
                var skew = TimeSpan.FromSeconds(Math.Max(0, refreshSkewSeconds));
                return ExpiresAt - skew > DateTimeOffset.UtcNow;
            }
        }
    }
}
