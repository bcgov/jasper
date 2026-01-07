using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Scv.Api.Services;
public interface IKeycloakTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}

public sealed class TdKeycloakTokenService : IKeycloakTokenService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<KeycloakOptions> _keycloakOptions;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TdKeycloakTokenService> _logger;

    private const string CacheKey = "KeycloakTokenService:AccessToken";
    private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1); // prevents concurrent token fetches
    private bool _disposed;

    public TdKeycloakTokenService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<KeycloakOptions> keycloakOptions,
        IMemoryCache cache,
        ILogger<TdKeycloakTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Fast path: return cached token if still valid
        if (TryGetValidCachedToken(out var cachedToken))
        {
            return cachedToken;
        }

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check pattern: another thread may have refreshed while we waited
            if (TryGetValidCachedToken(out cachedToken))
            {
                return cachedToken;
            }

            return await RefreshAndCacheTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Attempts to retrieve a valid (non-expiring) token from the cache.
    /// </summary>
    /// <param name="token">The cached access token if valid, otherwise null.</param>
    /// <returns>True if a valid token was found in cache, false otherwise.</returns>
    private bool TryGetValidCachedToken(out string token)
    {
        if (_cache.TryGetValue<TokenCacheEntry>(CacheKey, out var entry)
            && !IsExpiringSoon(entry, _keycloakOptions.CurrentValue))
        {
            token = entry.AccessToken;
            return true;
        }

        token = null;
        return false;
    }

    /// <summary>
    /// Fetches a new token from Keycloak and caches it with appropriate expiration.
    /// </summary>
    private async Task<string> RefreshAndCacheTokenAsync(CancellationToken ct)
    {
        var tokenResponse = await FetchNewTokenAsync(ct).ConfigureAwait(false);
        var expiresAt = ComputeExpiry(tokenResponse);

        var entry = new TokenCacheEntry(tokenResponse.AccessToken, expiresAt);

        _cache.Set(CacheKey, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        });

        _logger.LogDebug("Cached new access token (expires at {ExpiresAt}).", expiresAt);

        return entry.AccessToken;
    }

    private static bool IsExpiringSoon(TokenCacheEntry entry, KeycloakOptions opts)
    {
        var now = DateTimeOffset.UtcNow;
        var bufferSeconds = opts.RefreshSkewSeconds + opts.ClockSkewSeconds;
        var timeUntilExpiry = entry.ExpiresAtUtc - now;
        return timeUntilExpiry.TotalSeconds <= bufferSeconds;
    }

    private async Task<TokenResponse> FetchNewTokenAsync(CancellationToken ct)
    {
        var opts = _keycloakOptions.CurrentValue;

        if (string.IsNullOrWhiteSpace(opts.Authority))
            throw new InvalidOperationException("Keycloak Authority is not configured.");
        if (string.IsNullOrWhiteSpace(opts.ClientId))
            throw new InvalidOperationException("Keycloak ClientId is not configured.");

        var http = _httpClientFactory.CreateClient("keycloak");

        var tokenEndpoint = $"{opts.Authority.TrimEnd('/')}/protocol/openid-connect/token";

        var parameters = new Parameters();
        if (!string.IsNullOrWhiteSpace(opts.Audience))
        {
            parameters.Add("audience", opts.Audience);
        }

        var request = new ClientCredentialsTokenRequest
        {
            Address = tokenEndpoint,
            ClientId = opts.ClientId,
            ClientSecret = opts.Secret,
            Scope = string.IsNullOrWhiteSpace(opts.Scope) ? null : opts.Scope,
            Parameters = parameters
        };
        _logger.LogDebug("Requesting new service account token from Keycloak with clientId: {ClientId} and address: {Address}.", request.ClientId, request.Address);

        var response = await http.RequestClientCredentialsTokenAsync(request, ct).ConfigureAwait(false);
        if (response.IsError || string.IsNullOrWhiteSpace(response.AccessToken))
        {
            _logger.LogError("Failed to obtain access token from Keycloak: {Error}", response.Error);
            throw new InvalidOperationException(
                $"Failed to obtain access token from Keycloak: {response.Error ?? "unknown error"}");
        }

        _logger.LogInformation("Obtained new service account token (expires in {Seconds}s).", response.ExpiresIn);
        return response;
    }

    private static DateTimeOffset ComputeExpiry(TokenResponse token)
    {
        if (token.ExpiresIn > 0)
        {
            return DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.AccessToken);
        var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim == null || !long.TryParse(expClaim.Value, out var expUnix))
        {
            throw new InvalidOperationException("JWT token does not contain a valid 'exp' claim");
        }

        return DateTimeOffset.FromUnixTimeSeconds(expUnix);
    }

    private sealed record TokenCacheEntry(string AccessToken, DateTimeOffset ExpiresAtUtc);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _tokenLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
