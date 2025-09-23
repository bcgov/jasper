using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using Scv.Db.Interceptors;
using Scv.Db.Models;

namespace Scv.Db.Contexts
{
    public class JasperDbContext(DbContextOptions<JasperDbContext> options) : DbContext(options)
    {
        public DbSet<Permission> Permissions { get; init; }
        public DbSet<Role> Roles { get; init; }
        public DbSet<Group> Groups { get; init; }
        public DbSet<GroupAlias> GroupAliases { get; init; }
        public DbSet<User> Users { get; init; }
        public DbSet<Binder> Binders { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<ReservedJudgement> ReservedJudgements { get; set; }

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
            modelBuilder.Entity<GroupAlias>();
            modelBuilder.Entity<User>(u =>
            {
                u.HasKey(u => u.Id);
                u.HasIndex(u => u.Email).IsUnique();
                u.ToCollection("users");
            });
            modelBuilder.Entity<Tag>(t => t.HasKey(jb => jb.Id));
            modelBuilder.Entity<Binder>(jb => jb.HasKey(jb => jb.Id));
            modelBuilder.Entity<DocumentCategory>(dc => dc.HasKey(c => c.Id));
            modelBuilder.Entity<ReservedJudgement>(rj => rj.HasKey(r => r.Id));
        }
    }
}
