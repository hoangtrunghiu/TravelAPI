using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models;

namespace TravelAPI.Data
{
    public class TravelDbContext : IdentityDbContext<AppUser>
    {
        public TravelDbContext(DbContextOptions<TravelDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Slug);
            });
        }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}
