using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Slug { get; set; } = string.Empty;
        
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; //trạng thái đã xóa

        // Self-referencing for parent-child categories
        public int? ParentCategoryId { get; set; }

        [ForeignKey("ParentCategoryId")]
        public virtual Category? ParentCategory { get; set; }

        public virtual ICollection<Category> CategoryChildren { get; set; } = new List<Category>();
    }


    //lấy thuộc tính cha của category cho breadcrumb
    public class ParentCategoryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    }

    //lấy thuộc tính con của category
    public class ChildCategoryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    }


}
