using System.ComponentModel.DataAnnotations;
//Sử dụng thư viện ảnh cho các bảng
namespace TravelAPI.Models {
    public class LibraryImage
    {
        [Key]
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // Liên kết với bảng nào (Category, Product, Post)
        public int EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty; // "Category", "Product", "Post", "CategoryTour", "TourDetail"
    }

    public class ImageUrlDto
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class LibraryImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

}