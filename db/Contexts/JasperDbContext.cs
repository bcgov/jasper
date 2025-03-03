using Microsoft.EntityFrameworkCore;
using Scv.Db.Models;

namespace Scv.Db.Contexts
{
    public class JasperDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Permission> Permissions { get; init; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (this.Database != null)
            {
                this.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Permission>();
        }
    }
}
