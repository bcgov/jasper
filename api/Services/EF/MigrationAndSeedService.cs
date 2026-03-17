using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Seeders;

namespace Scv.Api.Services.EF
{
    /// <summary>
    /// This is a utility service, to load up our Migrations before any code execution. 
    /// </summary>
    public class MigrationAndSeedService(IServiceProvider services, ILogger<MigrationAndSeedService> logger)
    {
        public IServiceProvider Services { get; } = services;
        private ILogger<MigrationAndSeedService> Logger { get; } = logger;

        public async Task ExecuteMigrationsAndSeeds()
        {
            await this.ExecuteSCVMigrationsAndSeeds();
            await this.ExecuteJasperMigrationsAndSeeds();
        }

        #region JASPER Migrations and Seeds

        private async Task ExecuteJasperMigrationsAndSeeds()
        {
            try
            {
                Logger.LogInformation("Starting JASPER database migrations and seeding...");

                /* Migration needs to be handled manually. Will have to come up with a strategy
                 * to support adding, editing or deleting of fields, etc. This would require
                 * more dev work but would give us greater flexibility. 
                */

                // Seed
                using var scope = this.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<JasperDbContext>();
                var seederFactory = scope.ServiceProvider.GetRequiredService<SeederFactory<JasperDbContext>>();
                await seederFactory.SeedAsync(context);

                Logger.LogInformation("JASPER migrations and seeding completed.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "JASPER migrations and seeding failed.");
            }
        }

        #endregion JASPER Migrations and Seeds

        #region SCV Migrations and Seeds

        private async Task ExecuteSCVMigrationsAndSeeds()
        {
            try
            {
                Logger.LogInformation("Starting Migrations.");
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ScvDbContext>();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var migrationsAssembly = db.GetService<IMigrationsAssembly>();
                var historyRepository = db.GetService<IHistoryRepository>();

                var all = migrationsAssembly.Migrations.Keys;
                var applied = historyRepository.Exists() ? historyRepository.GetAppliedMigrations().Select(r => r.MigrationId).ToList() : new List<string>();
                var pending = all.Except(applied).ToList();
                Logger.LogInformation($"Pending {pending.Count} Migrations.");
                Logger.LogDebug($"{string.Join(", ", pending)}");

                // Use async migration with timeout
                await db.Database.MigrateAsync();
                Logger.LogInformation("Migration(s) complete.");

                if (applied.Count != 0) return;

                await ExecuteSeedScripts(db, environment);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Database migration failed on startup.");
                throw new InvalidOperationException("Database migration failed on startup.", ex);
            }
        }

        private async Task ExecuteSeedScripts(DbContext db, IWebHostEnvironment environment)
        {
            var seedPath = environment.IsDevelopment() ? Path.Combine("docker", "seed") : "data";
            var dbSqlPath = environment.IsDevelopment() ? Path.Combine("db", "sql") : Path.Combine("src", "db", "sql");
            var path = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, seedPath);
            var dbPath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, dbSqlPath);
            Logger.LogInformation($"Fresh database detected. Loading SQL from paths: {dbPath} and then {path}");

            using var transaction = await db.Database.BeginTransactionAsync();
            var lastFile = "";
            try
            {
                var files = GetSqlFilesOrderedByNumber(dbPath).Concat(GetSqlFilesOrderedByNumber(path)).ToList();
                Logger.LogInformation($"Found {files.Count} files.");
                foreach (var file in files)
                {
                    lastFile = file;
                    Logger.LogInformation($"Executing File: {file}");
                    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(file));
                }
                await transaction.CommitAsync();
                Logger.LogInformation($"Executing files successful.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while executing {lastFile}. Rolling back all files.");
                await transaction.RollbackAsync();
            }
        }

        private IEnumerable<string> GetSqlFilesOrderedByNumber(string path)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, "*.sql").OrderBy(x =>
                    Regex.Match(x, @"\d+").Value);

            Logger.LogWarning($"{path} does not exist.");
            return new List<string>();
        }

        #endregion SCV Migrations and Seeds
    }
}
