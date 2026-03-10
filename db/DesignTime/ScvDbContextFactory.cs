using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Scv.Db.Models;

namespace Scv.Db.DesignTime
{
    public sealed class ScvDbContextFactory : IDesignTimeDbContextFactory<ScvDbContext>
    {
        public ScvDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "DatabaseConnectionString is not set. Set it as an environment variable before running dotnet ef.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ScvDbContext>();
            optionsBuilder.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

            return new ScvDbContext(optionsBuilder.Options);
        }
    }
}
