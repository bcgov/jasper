using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Jobs;

namespace Scv.Api.Services
{
    /// <summary>
    /// Background service that registers Hangfire recurring jobs after the application has fully started.
    /// This prevents blocking the application startup if job dependencies require database connections.
    /// </summary>
    public class HangfireJobRegistrationService(
        IServiceProvider serviceProvider,
        ILogger<HangfireJobRegistrationService> logger,
        IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<HangfireJobRegistrationService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Wait a bit to ensure the application has fully started
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                _logger.LogInformation("Registering Hangfire recurring jobs...");

                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider;
                var allJobs = provider.GetServices<IRecurringJob>();
                var retryCount = configuration.GetValue<int>("JOBS:RETRY_COUNT", AutomaticRetryAttribute.DefaultRetryAttempts);

                GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = retryCount });

                foreach (var job in allJobs)
                {
                    RecurringJobHelper.AddOrUpdate(job);

                    _logger.LogInformation("Registered recurring job: {JobType}", job.GetType().Name);
                }

                _logger.LogInformation("All Hangfire recurring jobs registered successfully with retry count {RetryCount}", retryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register Hangfire recurring jobs");
            }
        }
    }
}