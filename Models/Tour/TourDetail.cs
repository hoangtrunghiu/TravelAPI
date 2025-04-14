using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TravelAPI.Models.Tour
{
    [Table("TourDetails")]
    public class TourDetail
    {
        [Key]
        public int Id { get; set; }
        [StringLength(160)]
        public string CodeTour { get; set; } = string.Empty; // Mã Tour
        [StringLength(255)]
        public string NameTour { get; set; } = string.Empty; // Tên Tour
        public decimal? OriginalPrice { get; set; } // Giá gốc Tour
        public decimal? PromotionallPrice { get; set; } // Giá khuyến mãi Tour
        public int CountryFrom { get; set; } // Quốc gia xuất phát
        public int? CountryTo { get; set; } // Quốc gia cuối cùng trong lịch trình
        [StringLength(160)]
        public string? Hotel { get; set; } = string.Empty; // Khách sạn
        [StringLength(160)]
        public string? Flight { get; set; } = string.Empty; // Hãng bay
        [StringLength(500)]
        public string? Notes { get; set; } = string.Empty; // Ghi chú đặc biệt
        [StringLength(160)]
        public string? Timeline { get; set; } = string.Empty; // Thời gian (VD: "11 Ngày 10 Đêm")
        public string? Description { get; set; } // Mô tả
        [StringLength(255)]
        public string Url { get; set; } = string.Empty; // Đường dẫn
        public string? Promotion { get; set; } = string.Empty; // Khuyến mãi
        // public string Color { get; set; } = "#000000"; // Màu sắc nhận diện
        public string? Avatar { get; set; } //Hình đại diện

        public DateTime CreateAt { get; set; } = DateTime.UtcNow; // Ngày tạo
        public string Creater { get; set; } = string.Empty;// Người tạo
        public bool IsDelete { get; set; } = false; // Trạng thái xóa
        public bool IsHot { get; set; } = false; // Tour hot hay không
        public bool IsHide { get; set; } = false; // Ẩn/hiện trên website

        // Danh mục chính
        public int? MainCategoryTourId { get; set; }
        [ForeignKey("MainCategoryTourId")]
        public CategoryTour? MainCategoryTour { get; set; }
        
        [JsonIgnore] // Bỏ khỏi JSON để tránh vòng lặp
        public virtual ICollection<TourCategoryMapping>? TourCategoryMappings { get; set; } // Danh mục tour liên quan
        [JsonIgnore]
        public virtual ICollection<TourDeparture>? TourDepartures { get; set; } // Các điểm khởi hành
        [JsonIgnore]
        public virtual ICollection<TourDestination>? TourDestinations { get; set; } // Các điểm đến
        [JsonIgnore]
        public ICollection<TourDate>? TourDates { get; set; } = new List<TourDate>(); // Danh sách ngày khởi hành
    }

    public class TourDetailDTO
    {
        public int Id { get; set; }
        public string CodeTour { get; set; } = string.Empty;
        public string NameTour { get; set; } = string.Empty;
        public decimal? OriginalPrice { get; set; }
        public decimal? PromotionallPrice { get; set; }
        public int? CountryFrom { get; set; }
        public int? CountryTo { get; set; }
        public string? Hotel { get; set; }
        public string? Flight { get; set; }
        public string? Notes { get; set; }
        public string? Timeline { get; set; }
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Promotion { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreateAt { get; set; }
        public string Creater { get; set; } = string.Empty;
        public bool IsHot { get; set; }
        public bool IsHide { get; set; }
        public int? MainCategoryTourId { get; set; }
        public string? MainCategoryTourName { get; set; }
        public List<ChildTourDTO> RelatedCategories { get; set; } = new();
        public List<ChildTourDTO> RelatedDestinations { get; set; } = new();
        public List<ChildTourDTO> RelatedDeparturePoints { get; set; } = new();
        public List<TourDateDTO> RelatedTourDates { get; set; } = new();
    }

    public class ChildTourDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class TourDateDTO
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class CreateTourDetailDTO
    {
        public string CodeTour { get; set; } = string.Empty;
        public string NameTour { get; set; } = string.Empty;
        public decimal? OriginalPrice { get; set; }
        public decimal? PromotionallPrice { get; set; }
        public int CountryFrom { get; set; }
        public int? CountryTo { get; set; }
        public string? Hotel { get; set; }
        public string? Flight { get; set; }
        public string? Notes { get; set; }
        public string? Timeline { get; set; }
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Promotion { get; set; }
        public string? Avatar { get; set; }
        public string Creater { get; set; } = string.Empty;
        public bool IsHot { get; set; }
        public bool IsHide { get; set; }
        public int MainCategoryTourId { get; set; }
        public List<int> RelatedCategoryIds { get; set; } = new();
        public List<int> RelatedDestinationIds { get; set; } = new();
        public List<int> RelatedDeparturePointIds { get; set; } = new();
    }



}