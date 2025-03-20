using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//dotnet aspnet-codegenerator controller -name MenusController -async -api -m TravelAPI.Models.Menu -dc TravelAPI.Data.TravelDbContext -outDir Controllers
namespace TravelAPI.Models
{
    [Table("Menus")]
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string? MenuName { get; set; }

        [Required, MaxLength(500)]
        public string? MenuUrl { get; set; }

        public int IndexNumber { get; set; } = 0; // Mặc định là 0

        public int? ParentId { get; set; } // Menu cha (nếu có)

        public bool IsHide { get; set; } = false; // Mặc định là hiển thị

        public bool IsDelete { get; set; } = false; // Dùng để soft delete
    }
}
