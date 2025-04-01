using System.ComponentModel.DataAnnotations.Schema;

//tạo bảng này cho mqh nhiều nhiều giữa tour và category tour
namespace TravelAPI.Models.Tour
{
    [Table("TourCategoryMapping")]
    public class TourCategoryMapping
    {
        public int TourDetailId { get; set; }
        public int CategoryTourId { get; set; }

        [ForeignKey("TourDetailId")]
        public virtual TourDetail TourDetail { get; set; } = null!;

        [ForeignKey("CategoryTourId")]
        public virtual CategoryTour CategoryTour { get; set; } = null!;
    }

}