using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TravelAPI.Models
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public int PostId { set; get; }

        [Required(ErrorMessage = "Phải có tiêu đề bài viết")]
        [StringLength(160, MinimumLength = 5, ErrorMessage = "{0} dài {1} đến {2}")]
        public string Title { set; get; }

        public string? Description { set; get; }

        [StringLength(160, MinimumLength = 5, ErrorMessage = "{0} dài {1} đến {2}")]
        [RegularExpression(@"^[a-z0-9-]*$", ErrorMessage = "Chỉ dùng các ký tự [a-z0-9-]")]
        public string Slug { set; get; }

        public string? Content { set; get; }

        public bool Published { set; get; }

        // public string? AuthorId { set; get; }
        // [ForeignKey("AuthorId")]
        // public AppUser? Author { set; get; }

        public DateTime DateCreated { set; get; } = DateTime.UtcNow;

        public DateTime DateUpdated { set; get; } = DateTime.UtcNow;

        // Danh mục chính
        public int? MainCategoryId { get; set; }
        [ForeignKey("MainCategoryId")]
        public Category? MainCategory { get; set; }

        // Danh mục liên quan
        [JsonIgnore] // Bỏ khỏi JSON để tránh vòng lặp
        public virtual ICollection<PostCategory>? PostCategories { get; set; }
    }

    public class PostDTO
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Content { set; get; }
        public bool Published { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public int? MainCategoryId { get; set; }
        public string? MainCategoryName { get; set; }

        public List<CateDTO> RelatedCategories { get; set; } = new();
    }

    public class CateDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
    public class CreatePostDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Content { set; get; }
        public bool Published { get; set; }
        public int MainCategoryId { get; set; }
        public List<int> RelatedCategoryIds { get; set; } = new();
    }



}