using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Scv.Db.Models.Auth;
using SS.Db;
using System;

namespace Scv.Db.Models
{
    public class ScvDbContext : DbContext, IDataProtectionKeyContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ScvDbContext()
        {

        }

        public ScvDbContext(DbContextOptions<ScvDbContext> options, IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // This maps to the table that stores keys.
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<RequestFileAccess> RequestFileAccess { get; set; }
        public DbSet<Audit> Audit { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyAllConfigurations();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql("Name=DatabaseConnectionString");
        }

        public TEntity DetachedClone<TEntity>(TEntity entity) where TEntity : class
            => Entry(entity).CurrentValues.Clone().ToObject() as TEntity;

        private Guid? GetUserId(string claimValue)
        {
            if (claimValue == null)
                return null;
            return Guid.Parse(claimValue);
        }
    }
}
