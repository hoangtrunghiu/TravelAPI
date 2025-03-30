using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models;
using TravelAPI.Models.Files;

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
            // Cấu hình quan hệ giữa tệp và thư mục
            builder.Entity<FileEntity>()
                .HasOne(f => f.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            // Đánh chỉ mục INDEX cột Slug bảng Category trong db => tìm kiếm nhanh hơn
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(p => p.Slug)
                      .IsUnique();
            });
            //tao khoa chinh cho postcategory tu post id va categoryid, vd postid = 5, category la 6. se co khoa chinh la 5 6 va khong co du lieu nao co khoa chinh 5 6 nua
            builder.Entity<PostCategory>(entity =>
            {
                entity.HasKey(c => new { c.PostId, c.CategoryId });
            });

            // Đánh chỉ mục INDEX cột Slug bảng Post trong db => tìm kiếm nhanh hơn
            builder.Entity<Post>(entity =>
            {
                entity.HasIndex(p => p.Slug)
                      .IsUnique(); //thiet lap chi muc nay la duy nhat, khong duoc phep co 2 bai post co slug giong nhau
            });
        }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Category> Categories { get; set; }

        public DbSet<FileEntity> Files { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
    }
}
