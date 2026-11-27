using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle communicating with PCSS auth services, and caching that information.
    /// </summary>
    public class PcssAuthorizationService : IPcssAuthorizationService
    {
        #region Variables

        private const string UsersCacheKey = "PCSS:Users";
        private static readonly TimeSpan UserListTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan FallbackWindow = TimeSpan.FromMinutes(5);
        // The scoped service and the priming job share the same LazyCache instance.
        private static readonly ConditionalWeakTable<IAppCache, UserCacheState> CacheStates = new();
        private readonly IAppCache _cache;
        private readonly PCSSAuthServices.IAuthorizationServicesClient _pcssAuthorizationServiceClient;
        private readonly ILogger<PcssAuthorizationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _freshDuration;
        private readonly UserCacheState _cacheState;

        #endregion Variables

        #region Constructor

        public PcssAuthorizationService(
            IConfiguration configuration,
            PCSSAuthServices.IAuthorizationServicesClient pcssAuthorizationServiceClient,
            IAppCache cache,
            ILogger<PcssAuthorizationService> logger,
            IHttpContextAccessor httpContextAccessor = null,
            TimeProvider timeProvider = null
        )
        {
            _pcssAuthorizationServiceClient = pcssAuthorizationServiceClient;
            _cache = cache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _freshDuration = TimeSpan.FromMinutes(int.Parse(configuration.GetNonEmptyValue("Caching:UserExpiryMinutes")));
            _cacheState = CacheStates.GetValue(cache, _ => new UserCacheState());
        }

        #endregion Constructor

        private CancellationToken RequestAborted => _httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;

        public Task<ICollection<PCSSAuthServices.UserItem>> GetUsers() => GetFreshUsers(RequestAborted);

        public Task<ICollection<PCSSAuthServices.UserItem>> RefreshUsers(CancellationToken cancellationToken = default) =>
            FetchAndCacheUsers(cancellationToken);

        public async Task<PcssUserMatch> GetUserByGuid(string userGuid, bool forceRefresh = false)
        {
            var cancellationToken = RequestAborted;
            var matchingUser = await FindUserByGuid(userGuid, forceRefresh: false, cancellationToken);

            // A user newly added to PCSS may be missing from the cached list.
            // On a cache miss, the initial fetch and forced refresh can each take up to 60 seconds.
            if (matchingUser is null && forceRefresh)
            {
                _logger.LogDebug("Force refreshing cache for userGuid: {UserGuid}", userGuid);
                matchingUser = await FindUserByGuid(userGuid, forceRefresh: true, cancellationToken);
            }
            return matchingUser;
        }

        public async Task<OperationResult<IEnumerable<string>>> GetPcssUserRoleNames(int userId, string expectedGuid = null)
        {
            _logger.LogInformation("Fetching roles for user with ID {UserId}.", userId);
            var user = await this._pcssAuthorizationServiceClient.GetUserAsync(userId, CancellationToken.None);

            if (expectedGuid != null && (user?.UserId != userId || string.IsNullOrWhiteSpace(user.GUID) ||
                !string.Equals(user.GUID, expectedGuid, StringComparison.Ordinal)))
            {
                _logger.LogWarning("The individual PCSS user response did not confirm the fallback identity mapping.");
                return OperationResult<IEnumerable<string>>.Failure("The PCSS user identity could not be confirmed.");
            }

            var userRoles = user?.Roles;

            var userRoleNames = userRoles?
                .Where(IsRoleActive)
                .Select(ur => ur.Name)
                .Distinct();

            if (userRoleNames == null || !userRoleNames.Any())
            {
                _logger.LogWarning("No roles found for user with ID {UserId}.", userId);
                return OperationResult<IEnumerable<string>>.Failure("User not found or has no roles.");
            }

            _logger.LogInformation("Roles successfully fetched for user with ID {UserId}.", userId);
            _logger.LogDebug("Fetched Roles {UserId}. {Roles}", userId, userRoles);
            return OperationResult<IEnumerable<string>>.Success(userRoleNames);
        }

        public async Task<OperationResult<PCSSAuthServices.UserItem>> GetPcssUserById(int userId)
        {
            _logger.LogInformation("Fetching user with ID {UserId} from cache or PCSS.", userId);
            var users = await GetUsers();
            var user = users?.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
                return OperationResult<PCSSAuthServices.UserItem>.Failure("User not found.");
            }

            _logger.LogInformation("User with ID {UserId} successfully fetched.", userId);
            return OperationResult<PCSSAuthServices.UserItem>.Success(user);
        }

        #region Helpers

        /// <summary>
        /// Determines if a role is currently active based on its effective and expiry dates.
        /// A role is active if:
        /// - It has no expiry date OR the expiry date is in the future, AND
        /// - The effective date is in the past or present
        /// </summary>
        private static bool IsRoleActive(PCSSAuthServices.EffectiveRoleItem role)
        {
            var isNotExpired = IsRoleNotExpired(role.ExpiryDate);
            var isEffective = IsRoleEffective(role.EffectiveDate);

            return isNotExpired && isEffective;
        }

        /// <summary>
        /// Checks if a role has not expired.
        /// Returns true if the expiry date is null or in the future.
        /// </summary>
        private static bool IsRoleNotExpired(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate))
                return true;

            if (DateTime.TryParse(expiryDate, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedExpiryDate))
            {
                return parsedExpiryDate >= DateTime.Now;
            }

            return false;
        }

        /// <summary>
        /// Checks if a role's effective date has been reached.
        /// Returns true if the effective date is in the past or present.
        /// </summary>
        private static bool IsRoleEffective(string effectiveDate)
        {
            if (string.IsNullOrEmpty(effectiveDate))
                return false;

            if (DateTime.TryParse(effectiveDate, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedEffectiveDate))
            {
                return parsedEffectiveDate <= DateTime.Now;
            }

            return false;
        }

        private async Task<PcssUserMatch> FindUserByGuid(
            string userGuid, bool forceRefresh, CancellationToken cancellationToken)
        {
            try
            {
                var users = forceRefresh
                    ? await RefreshUsers(cancellationToken)
                    : await GetFreshUsers(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var match = users?.FirstOrDefault(user => user.GUID == userGuid);
                return match == null ? null : new PcssUserMatch(match);
            }
            catch (UserListTimeoutException ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshot = ReadUserListSnapshot();
                var now = _timeProvider.GetUtcNow();
                var eligible = snapshot != null && now < snapshot.ExpiresAt;
                _logger.Log(eligible ? LogLevel.Warning : LogLevel.Debug, ex,
                    "PCSS user-list fallback check. Reason: {Reason}. Cached data age in seconds: {CacheAgeSeconds}. Eligible fallback: {FallbackAvailable}.",
                    "User list request deadline exceeded", (now - snapshot?.RetrievedAt)?.TotalSeconds, eligible);

                if (!eligible)
                {
                    throw;
                }

                var match = snapshot.Users.FirstOrDefault(user => user.GUID == userGuid);
                return match == null ? null : new PcssUserMatch(match, UsedFallback: true);
            }
        }

        private async Task<ICollection<PCSSAuthServices.UserItem>> GetFreshUsers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task<ICollection<PCSSAuthServices.UserItem>> pendingRead;
            lock (_cacheState)
            {
                var snapshot = ReadFreshUserListSnapshot();
                if (snapshot != null)
                {
                    return snapshot.Users;
                }

                pendingRead = _cacheState.PendingRead ?? PopulateUsersCache();
                // A synchronously completed fetch has already cleared the pending read.
                _cacheState.PendingRead = pendingRead.IsCompleted ? null : pendingRead;
            }

            // Cancel this caller's wait independently of the shared read's request deadline.
            return await pendingRead.WaitAsync(cancellationToken);
        }

        private UserListSnapshot ReadUserListSnapshot() =>
            _cache.TryGetValue<UserListSnapshot>(UsersCacheKey, out var snapshot) ? snapshot : null;

        private UserListSnapshot ReadFreshUserListSnapshot()
        {
            var snapshot = ReadUserListSnapshot();
            return snapshot != null && _timeProvider.GetUtcNow() < snapshot.FreshUntil ? snapshot : null;
        }

        private async Task<ICollection<PCSSAuthServices.UserItem>> PopulateUsersCache()
        {
            try
            {
                return await FetchAndCacheUsers(CancellationToken.None);
            }
            finally
            {
                lock (_cacheState)
                {
                    _cacheState.PendingRead = null;
                }
            }
        }

        private async Task<ICollection<PCSSAuthServices.UserItem>> FetchAndCacheUsers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var version = Interlocked.Increment(ref _cacheState.NextVersion);
            using var deadline = new CancellationTokenSource(UserListTimeout, _timeProvider);
            using var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            ICollection<PCSSAuthServices.UserItem> users;
            try
            {
                _logger.LogInformation("Fetching users from PCSS.");
                users = await _pcssAuthorizationServiceClient.GetUsersAsync(request.Token);
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The PCSS user list request was canceled.", ex, cancellationToken);
            }
            catch (OperationCanceledException ex) when (deadline.IsCancellationRequested && ex.CancellationToken == request.Token)
            {
                throw new UserListTimeoutException(ex);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (users == null)
            {
                return null;
            }

            if (users.Any(user => user == null))
            {
                throw new InvalidDataException("The PCSS user list contains a null user.");
            }

            var retrievedAt = _timeProvider.GetUtcNow();
            var freshUntil = retrievedAt.Add(_freshDuration);
            var snapshot = new UserListSnapshot(users, retrievedAt, freshUntil, freshUntil.Add(FallbackWindow));
            lock (_cacheState)
            {
                if (version < _cacheState.PublishedVersion)
                {
                    // A later request has already published its authoritative response.
                    return ReadFreshUserListSnapshot()?.Users;
                }

                _cache.Add(UsersCacheKey, snapshot, snapshot.ExpiresAt);
                _cacheState.PublishedVersion = version;
                return users;
            }
        }

        private sealed record UserListSnapshot(
            ICollection<PCSSAuthServices.UserItem> Users,
            DateTimeOffset RetrievedAt,
            DateTimeOffset FreshUntil,
            DateTimeOffset ExpiresAt);

        private sealed class UserCacheState
        {
            public long NextVersion;
            public long PublishedVersion;
            public Task<ICollection<PCSSAuthServices.UserItem>> PendingRead;
        }

        private sealed class UserListTimeoutException(OperationCanceledException innerException)
            : TimeoutException("The PCSS user list request exceeded its deadline.", innerException)
        {
        }

        #endregion Helpers
    }
}
