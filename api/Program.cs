using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scv.Api.Services.EF;

namespace Scv.Api
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<object>>();

            try
            {
                logger.LogInformation("Application starting in {Environment} environment",
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

                await RunMigrationsAsync(host, logger);

                logger.LogInformation("Starting web host");
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application terminated unexpectedly");
                throw new InvalidOperationException("Application terminated unexpectedly. See inner exception for details.", ex);
            }
        }

        private static async Task RunMigrationsAsync(IHost host, ILogger logger)
        {
            logger.LogInformation("Starting database migrations and seeding");

            var migrationService = host.Services.GetRequiredService<MigrationAndSeedService>();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            try
            {
                await migrationService.ExecuteMigrationsAndSeeds()
                    .WaitAsync(cts.Token);

                logger.LogInformation("Database migrations and seeding completed successfully");
            }
            catch (TimeoutException ex)
            {
                logger.LogCritical(ex, "Database migrations timed out after 5 minutes");
                throw new InvalidOperationException(
                    "Database migrations and seeding timed out. Check database connectivity.", ex);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}