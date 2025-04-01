using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models.Tour
{
    [Table("CategoryTours")]
    public class CategoryTour
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string CategoryName { get; set; } = string.Empty; //Tên thể loại

        [Required, StringLength(255)]
        public string Topic { get; set; } = string.Empty; //Tên chủ đề chính

        [Required, StringLength(255)]
        public string Url { get; set; } = string.Empty; //Đường dẫn

        public string? Description { get; set; } //Mô tả ngắn

        public string? ContentIntro { get; set; } //Nội dung chi tiết đầu trang

        public string? ContentDetail { get; set; } //Nội dung chi tiết cuối trang

        public string? Avatar { get; set; } //Hình đại diện

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //Ngày tạo

        public string? Creator { get; set; } = string.Empty; // người tạo - lấy username hoặc email
        public string? CreatorName { get; set; } = string.Empty; //Tên người tạo
        public string? Editor { get; set; } = string.Empty; // người chỉnh sửa - lấy username hoặc email
        public string? EditorName { get; set; } = string.Empty; //Tên người chỉnh sửa

        //SEO 
        public string? MetaTitle { get; set; } //Tiêu đề SEO
        public string? MetaDescription { get; set; } //Mô tả SEO
        public string? MetaKeywords { get; set; } //Từ khóa SEO
        public bool? IsIndexRobot { get; set; } //Bật tắt lập chỉ mục

        public bool IsDeleted { get; set; } = false; //trạng thái đã xóa

        // Self-referencing for parent-child categories
        public int? ParentCategoryTourId { get; set; } //Danh mục cha nếu có

        [ForeignKey("ParentCategoryTourId")]
        public virtual CategoryTour? ParentCategoryTour { get; set; } //Set khóa ngoại tới danh mục cha

        public virtual ICollection<CategoryTour> CategoryTourChildren { get; set; } = new List<CategoryTour>(); //Các danh mục con nếu có
    }

    public class CategoryTourDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContentIntro { get; set; } 
        public string? ContentDetail { get; set; } 
        public string? Avatar { get; set; }
        public string? Creator { get; set; } = string.Empty;
        public string? CreatorName { get; set; } = string.Empty; 
        public string? Editor { get; set; } = string.Empty;
        public string? EditorName { get; set; } = string.Empty;
        public string? MetaTitle { get; set; } 
        public string? MetaDescription { get; set; } 
        public string? MetaKeywords { get; set; } 
        public bool? IsIndexRobot { get; set; } 
        public int? ParentCategoryTourId { get; set; }
        public string? ParentCategoryTourName { get; set; }
        public bool IsDeleted { get; set; }
        public List<CategoryTourDTO> Children { get; set; } = new List<CategoryTourDTO>();
    }

    //lấy thuộc tính cha của CategoryTour cho breadcrumb
    public class ParentCategoryTourDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Url { get; set; }
    }

    //lấy thuộc tính con của CategoryTour
    public class ChildCategoryTourDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Url { get; set; }
    }


}
