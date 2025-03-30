using System.ComponentModel.DataAnnotations.Schema;

//tạo bảng này cho mqh nhiều nhiều giữa post và category
namespace TravelAPI.Models
{
    [Table("PostCategory")]
    public class PostCategory
    {
        public int PostId { get; set; }
        public int CategoryId { get; set; }

        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
    }

}