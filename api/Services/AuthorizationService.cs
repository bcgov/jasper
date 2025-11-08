using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and LocationServicesClient.
    /// </summary>
    public class AuthorizationService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly PCSSAuthServices.AuthorizationServicesClient _pcssAuthorizationServiceClient;
        private readonly ILogger<AuthorizationService> _logger;

        #endregion Variables

        #region Constructor

        public AuthorizationService(
            IConfiguration configuration,
            PCSSAuthServices.AuthorizationServicesClient pcssAuthorizationServiceClient,
            IAppCache cache,
            ILogger<AuthorizationService> logger
        )
        {
            _pcssAuthorizationServiceClient = pcssAuthorizationServiceClient;
            _cache = cache;
            _logger = logger;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:UserExpiryMinutes")) * 60;
        }

        #endregion Constructor

        public async Task<ICollection<PCSSAuthServices.UserItem>> GetUsers() => await GetDataFromCache($"Users", async () =>
        {
            _logger.LogInformation("Fetching users from cache or PCSS.");
            return await this._pcssAuthorizationServiceClient.GetUsersAsync();
        });

        public async Task<OperationResult<IEnumerable<string>>> GetPcssUserRoleNames(int userId)
        {
            _logger.LogInformation("Fetching roles for user with ID {UserId}.", userId);
            var user = await this._pcssAuthorizationServiceClient.GetUserAsync(userId);
            var userRoles = user?.Roles;
            var userRoleNames = userRoles?.Select(ur => ur.Name).Distinct();
            if (userRoleNames == null || !userRoleNames.Any())
            {
                _logger.LogWarning("No roles found for user with ID {UserId}.", userId);
                return OperationResult<IEnumerable<string>>.Failure("User not found or has no roles.");
            }

            _logger.LogInformation("Roles successfully fetched for user with ID {UserId}.", userId);
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