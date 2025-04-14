//Ngày khởi hành
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models.Tour
{
    public class TourDate
    {
        public int Id { get; set; }
        public int TourDetailId { get; set; } //Mã Tour
        
        [ForeignKey("TourDetailId")]
        public virtual TourDetail TourDetail { get; set; } = null!;

        public DateTime StartDate { get; set; } // Ngày khởi hành cụ thể
    }

}