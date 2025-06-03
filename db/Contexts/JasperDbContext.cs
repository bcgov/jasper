using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using Scv.Db.Interceptors;
using Scv.Db.Models;

namespace Scv.Db.Contexts
{
    public class JasperDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Permission> Permissions { get; init; }
        public DbSet<Role> Roles { get; init; }
        public DbSet<Group> Groups { get; init; }
        public DbSet<User> Users { get; init; }
        public DbSet<JudicialBinder> JudicialBinders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // For local development
            if (this.Database != null)
            {
                this.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
            }

            optionsBuilder.AddInterceptors(new AuditInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Permission>();
            modelBuilder.Entity<Role>();
            modelBuilder.Entity<Group>();
            modelBuilder.Entity<User>(u =>
            {
                u.HasKey(u => u.Id);
                u.HasIndex(u => u.Email).IsUnique();
                u.ToCollection("users");
            });
            modelBuilder.Entity<JudicialBinder>(jb => jb.HasKey(jb => jb.Id));
        }
    }
}
