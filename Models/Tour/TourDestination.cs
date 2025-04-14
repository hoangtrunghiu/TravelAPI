using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models.Tour
{
    // Bảng trung gian nhiều-nhiều giữa Tour và TourDestination
    public class TourDestination
    {
        public int TourDetailId { get; set; }
        public int DestinationId { get; set; }

        [ForeignKey("TourDetailId")]
        public virtual TourDetail TourDetail { get; set; } = null!;

        [ForeignKey("DestinationId")]
        public virtual Destination Destination { get; set; } = null!;
    }

    //Model điểm đến, ví dụ Ý, Đức, Italy....
    public class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // VD: Pháp, Thụy Sĩ, Ý
        public int? ParentId { get; set; }//mục cha nếu có

        [ForeignKey("ParentId")]
        public virtual Destination? ParentDestination { get; set; }

        public virtual ICollection<Destination> DestinationChildren { get; set; } = new List<Destination>();
    }

    public class DestinationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public List<DestinationDTO> Children { get; set; } = new List<DestinationDTO>();
    }
}