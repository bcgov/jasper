using System;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using MapsterMapper;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;
using Hangfire;

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

        public override string CronSchedule
        {
            get
            {
                var cacheExpiryMinutes = int.Parse(this.Configuration.GetNonEmptyValue("Caching:UserExpiryMinutes"));
                return ConvertMinutesToCronExpression(cacheExpiryMinutes);
            }
        }

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

        private static string ConvertMinutesToCronExpression(int minutes)
        {
            if (minutes <= 0)
            {
                throw new ArgumentException("Cache expiry minutes must be greater than 0.", nameof(minutes));
            }

            if (minutes < 60)
            {
                return Cron.MinuteInterval(minutes);
            }

            if (minutes % 60 == 0 && minutes < 1440)
            {
                return Cron.HourInterval(minutes / 60);
            }

            if (minutes % 1440 == 0)
            {
                return Cron.DayInterval(minutes / 1440);
            }

            throw new ArgumentException("Cache expiry minutes must align to Hangfire minute, hour, or day intervals.", nameof(minutes));
        }
    }
}
