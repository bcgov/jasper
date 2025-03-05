using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scv.Api.Services.EF;

namespace Scv.Api
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            //This needs to run before our host, because it may have essential migrations for AddDataProtection to work. 
            var migrationService = host.Services.GetRequiredService<MigrationAndSeedService>();
            await migrationService.ExecuteMigrationsAndSeeds();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}