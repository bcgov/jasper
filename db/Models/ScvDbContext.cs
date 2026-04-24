using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scv.Db.Models.Auth;

namespace Scv.Db.Models
{
    public class ScvDbContext : DbContext, IDataProtectionKeyContext
    {

        public ScvDbContext()
        {

        }

        public ScvDbContext(DbContextOptions<ScvDbContext> options)
            : base(options)
        {
        }

        // This maps to the table that stores keys.
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<RequestFileAccess> RequestFileAccess { get; set; }
        public DbSet<Audit> Audit { get; set; }
        public DbSet<SignalROutboxMessage> SignalROutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyAllConfigurations();
            modelBuilder.Entity<SignalROutboxMessage>(entity =>
            {
                entity.ToTable("signalr_outbox_message");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql("Name=DatabaseConnectionString");
        }

        public TEntity DetachedClone<TEntity>(TEntity entity) where TEntity : class
            => Entry(entity).CurrentValues.Clone().ToObject() as TEntity;
    }
}
