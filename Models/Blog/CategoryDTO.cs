namespace TravelAPI.Models
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryTitle { get; set; }
        public List<CategoryDTO> Children { get; set; } = new List<CategoryDTO>();
    }
}

