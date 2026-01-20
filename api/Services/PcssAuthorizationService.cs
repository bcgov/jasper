using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle communicating with PCSS auth services, and caching that information.
    /// </summary>
    public class PcssAuthorizationService : IPcssAuthorizationService
    {
        #region Variables

        private const string UsersCacheKey = "Users";
        private readonly IAppCache _cache;
        private readonly PCSSAuthServices.IAuthorizationServicesClient _pcssAuthorizationServiceClient;
        private readonly ILogger<PcssAuthorizationService> _logger;

        #endregion Variables

        #region Constructor

        public PcssAuthorizationService(
            IConfiguration configuration,
            PCSSAuthServices.IAuthorizationServicesClient pcssAuthorizationServiceClient,
            IAppCache cache,
            ILogger<PcssAuthorizationService> logger
        )
        {
            _pcssAuthorizationServiceClient = pcssAuthorizationServiceClient;
            _cache = cache;
            _logger = logger;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:UserExpiryMinutes")) * 60;
        }

        #endregion Constructor

        public Task<ICollection<PCSSAuthServices.UserItem>> GetUsers() => GetUsersInternal(forceRefresh: false);

        public async Task<PCSSAuthServices.UserItem> GetUserByGuid(string userGuid, bool forceRefresh = false)
        {
            var pcssUsers = await GetUsers();
            var matchingUser = pcssUsers?.FirstOrDefault(pu => pu.GUID == userGuid);

            // This insures that the cache will not ignore brand new users just added to PCSS. Also updates the cache here after fetching.
            if (matchingUser is null && forceRefresh)
            {
                _logger.LogDebug("Force refreshing cache for userGuid: {UserGuid}", userGuid);
                var updatedPcssUsers = await GetUsersInternal(forceRefresh: true);
                matchingUser = updatedPcssUsers?.FirstOrDefault(pu => pu.GUID == userGuid);
            }
            return matchingUser;
        }

        public async Task<OperationResult<IEnumerable<string>>> GetPcssUserRoleNames(int userId)
        {
            _logger.LogInformation("Fetching roles for user with ID {UserId}.", userId);
            var user = await this._pcssAuthorizationServiceClient.GetUserAsync(userId);
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

        private async Task<ICollection<PCSSAuthServices.UserItem>> GetUsersInternal(bool forceRefresh)
        {
            if (!forceRefresh)
            {
                return await GetDataFromCache(UsersCacheKey, FetchUsersFromSource);
            }

            var refreshedUsers = await FetchUsersFromSource();
            UpdateUsersCache(refreshedUsers);
            return refreshedUsers;
        }

        private async Task<ICollection<PCSSAuthServices.UserItem>> FetchUsersFromSource()
        {
            _logger.LogInformation("Fetching users from PCSS.");
            return await _pcssAuthorizationServiceClient.GetUsersAsync();
        }

        private void UpdateUsersCache(ICollection<PCSSAuthServices.UserItem> updatedUsers)
        {
            if (updatedUsers == null)
            {
                return;
            }

            var cacheDuration = TimeSpan.FromSeconds(_cache.DefaultCachePolicy.DefaultCacheDurationSeconds);
            _cache.Add(UsersCacheKey, updatedUsers, cacheDuration);
        }

        private async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
        {
            _logger.LogDebug("Attempting to fetch data for key {CacheKey} from cache.", key);
            return await _cache.GetOrAddAsync(key, async () =>
            {
                _logger.LogDebug("Cache miss for key {CacheKey}. Fetching data.", key);
                return await fetchFunction.Invoke();
            });
        }

        #endregion Helpers
    }
}