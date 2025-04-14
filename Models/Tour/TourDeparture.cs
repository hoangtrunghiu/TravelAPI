using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models.Tour
{
    // Bảng trung gian nhiều-nhiều giữa Tour và DeparturePoint
    public class TourDeparture
    {
        public int TourDetailId { get; set; }
        public int DeparturePointId { get; set; }

        [ForeignKey("TourDetailId")]
        public virtual TourDetail TourDetail { get; set; } = null!;

        [ForeignKey("DeparturePointId")]
        public virtual DeparturePoint DeparturePoint { get; set; } = null!;
    }

    //Model điểm khởi hành, ví dụ Hà Nội, Hồ Chí Minh
    public class DeparturePoint
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // VD: Hồ Chí Minh, Hà Nội
    }
}