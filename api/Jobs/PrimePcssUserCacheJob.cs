using System;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using MapsterMapper;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Jobs
{
    public class PrimePcssUserCacheJob(
        IConfiguration configuration,
        IAppCache cache,
        IMapper mapper,
        ILogger<PrimePcssUserCacheJob> logger,
        PCSSAuthServices.IAuthorizationServicesClient pcssAuthorizationServiceClient) : RecurringJobBase<PrimePcssUserCacheJob>(configuration, cache, mapper, logger)
    {
        private readonly PCSSAuthServices.IAuthorizationServicesClient _pcssAuthorizationServiceClient = pcssAuthorizationServiceClient;

        public override string JobName => nameof(PrimePcssUserCacheJob);

        // Run every 8 hours by default
        public override string CronSchedule =>
            this.Configuration.GetValue<string>("JOBS:PRIME_PCSS_USER_CACHE_SCHEDULE") ?? "* */8 * * *";

        public override async Task Execute()
        {
            try
            {
                this.Logger.LogInformation("Starting to prime PCSS user cache.");

                // Fetch directly from client to bypass existing cache and ensure fresh data
                var users = await _pcssAuthorizationServiceClient.GetUsersAsync();

                var cacheDurationMinutes = int.Parse(this.Configuration.GetNonEmptyValue("Caching:UserExpiryMinutes"));
                var cacheDuration = TimeSpan.FromMinutes(cacheDurationMinutes);

                // Overwrite the cache
                this.Cache.Add("Users", users, cacheDuration);

                this.Logger.LogInformation("Successfully primed PCSS user cache with {Count} users.", users.Count);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error occurred while priming PCSS user cache.", ex);
            }
        }
    }
}
