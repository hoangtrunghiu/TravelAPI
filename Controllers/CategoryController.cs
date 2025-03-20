using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models;
using TravelAPI.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace App.Api.Controllers
{
    [Route("api/category")]
    [ApiController]
    // [Authorize(Roles = "Administrator")]
    public class CategoryController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public CategoryController(TravelDbContext context)
        {
            _context = context;
        }

        // GET: api/category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.CategoryChildren)
                .Include(c => c.ParentCategory)
                .Where(c => c.ParentCategoryId == null) // Lấy danh mục gốc
                .ToListAsync();

            return categories.Select(MapToDTO).ToList();
        }

        // GET: api/category/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDTO>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.CategoryChildren)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return MapToDTO(category);
        }

        // POST: api/category
        [HttpPost]
        public async Task<ActionResult<CategoryDTO>> CreateCategory(CategoryDTO categoryDTO)
        {
            if (categoryDTO.ParentCategoryId == -1)
                categoryDTO.ParentCategoryId = null;

            // Kiểm tra nếu slug đã tồn tại
            bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == categoryDTO.Slug);
            if (slugExists)
            {
                return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
            }
            
            var category = new Category
            {
                Title = categoryDTO.Title,
                Slug = categoryDTO.Slug,
                Description = categoryDTO.Description,
                ParentCategoryId = categoryDTO.ParentCategoryId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDTO(category));
        }

        // PUT: api/category/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDTO categoryDTO)
        {
            if (id != categoryDTO.Id)
                return BadRequest();

            if (categoryDTO.ParentCategoryId == id)
                return BadRequest(new { message = "Phải chọn danh mục cha khác." });

            bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == categoryDTO.Slug);
            if (slugExists)
            {
                return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Title = categoryDTO.Title;
            category.Slug = categoryDTO.Slug;
            category.Description = categoryDTO.Description;
            category.ParentCategoryId = categoryDTO.ParentCategoryId == -1 ? null : categoryDTO.ParentCategoryId;
            
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/category/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.CategoryChildren)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            // Di chuyển các danh mục con lên cấp cao hơn
            foreach (var child in category.CategoryChildren)
                child.ParentCategoryId = category.ParentCategoryId;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Hàm chuyển đổi Category -> CategoryDTO để tránh vòng lặp
        private CategoryDTO MapToDTO(Category category)
        {
            return new CategoryDTO
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryTitle = category.ParentCategory?.Title,
                Children = category.CategoryChildren.Select(MapToDTO).ToList()
            };
        }
    }
}
