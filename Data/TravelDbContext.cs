using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models;
using TravelAPI.Models.Files;
using TravelAPI.Models.Tour;

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
            // Đánh chỉ mục INDEX cột Url bảng CategoryTour trong db => tìm kiếm nhanh hơn
            builder.Entity<CategoryTour>(entity =>
            {
                entity.HasIndex(p => p.Url)
                      .IsUnique();
            });
            // Đánh chỉ mục INDEX cột Url bảng TourDetail trong db => tìm kiếm nhanh hơn
            builder.Entity<TourDetail>(entity =>
            {
                entity.HasIndex(p => p.Url)
                      .IsUnique();
            });
            //Tạo khóa chính cho bảng TourCategoryMapping
            builder.Entity<TourCategoryMapping>(entity =>
            {
                entity.HasKey(t => new { t.TourDetailId, t.CategoryTourId });
            });
            //Tạo khóa chính cho bảng TourDestination
            builder.Entity<TourDestination>(entity =>
            {
                entity.HasKey(t => new { t.DestinationId, t.TourDetailId });
            });
            //Tạo khóa chính cho bảng TourDeparture
            builder.Entity<TourDeparture>(entity =>
            {
                entity.HasKey(t => new { t.DeparturePointId, t.TourDetailId });
            });
        }
        public DbSet<Menu> Menus { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }

        public DbSet<FileEntity> Files { get; set; }
        public DbSet<Folder> Folders { get; set; }

        public DbSet<CategoryTour> CategoryTours { get; set; }
        public DbSet<TourDetail> TourDetails { get; set; }
        public DbSet<TourCategoryMapping> TourCategoryMappings { get; set; }
        public DbSet<TourDate> TourDates { get; set; }
        public DbSet<DeparturePoint> DeparturePoints { get; set; }
        public DbSet<TourDeparture> TourDepartures { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<TourDestination> TourDestinations { get; set; }


        public DbSet<LibraryImage> LibraryImages { get; set; }
    }
}
