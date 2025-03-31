using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models;
using TravelAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace TravelAPI.Controllers
{
    [Route("api/category")]
    [ApiController]
    [Authorize(Roles = "Administrator,Editor")]
    public class CategoryController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(TravelDbContext context, ILogger<CategoryController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.CategoryChildren) // Tải danh mục con cấp 2
                    .ThenInclude(c => c.CategoryChildren) // Tải danh mục con cấp 3
                    .Include(c => c.ParentCategory) // Lấy tên danh mục cha
                    .Where(c => c.ParentCategoryId == null) // Lấy danh mục gốc
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                return Ok(categories.Select(MapToDTO).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting categories");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách danh mục" });
            }
        }

        // GET: api/category/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDTO>> GetCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.Categories
                    .Include(c => c.CategoryChildren)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                return Ok(MapToDTO(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin danh mục" });
            }
        }

        // POST: api/category
        [HttpPost]
        public async Task<ActionResult<CategoryDTO>> CreateCategory([FromBody] CategoryDTO categoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(categoryDTO.Slug))
                {
                    return BadRequest(new { message = "URL slug không được để trống" });
                }

                if (categoryDTO.ParentCategoryId == -1)
                {
                    categoryDTO.ParentCategoryId = null;
                }
                else if (categoryDTO.ParentCategoryId.HasValue)
                {
                    // Kiểm tra xem danh mục cha có tồn tại không
                    var parentExists = await _context.Categories.AnyAsync(c => c.Id == categoryDTO.ParentCategoryId);
                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Danh mục cha không tồn tại" });
                    }
                }

                // Kiểm tra nếu slug đã tồn tại
                bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == categoryDTO.Slug);
                if (slugExists)
                {
                    return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
                }

                var category = new Category
                {
                    Title = categoryDTO.Title.Trim(),
                    Slug = categoryDTO.Slug.Trim().ToLower(),
                    Description = categoryDTO.Description?.Trim(),
                    ParentCategoryId = categoryDTO.ParentCategoryId
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created: {Title} (ID: {Id})", category.Title, category.Id);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToDTO(category));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating category");
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu khi tạo danh mục" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo danh mục" });
            }
        }

        // PUT: api/category/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO categoryDTO)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != categoryDTO.Id)
                {
                    return BadRequest(new { message = "ID trong URL không khớp với ID trong dữ liệu" });
                }

                if (string.IsNullOrWhiteSpace(categoryDTO.Slug))
                {
                    return BadRequest(new { message = "URL slug không được để trống" });
                }

                // Kiểm tra xem danh mục có tồn tại không
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during update", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                // Kiểm tra nếu danh mục cha là chính nó
                if (categoryDTO.ParentCategoryId == id)
                {
                    return BadRequest(new { message = "Danh mục không thể là cha của chính nó" });
                }

                // Kiểm tra xem danh mục cha có tồn tại không
                if (categoryDTO.ParentCategoryId.HasValue && categoryDTO.ParentCategoryId != -1)
                {
                    bool parentExists = await _context.Categories.AnyAsync(c => c.Id == categoryDTO.ParentCategoryId);
                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Danh mục cha không tồn tại" });
                    }

                    // Kiểm tra nếu danh mục cha là con của danh mục hiện tại (tránh vòng lặp)
                    if (await IsCategoryChildOf(id, categoryDTO.ParentCategoryId.Value))
                    {
                        return BadRequest(new { message = "Không thể chọn một danh mục con làm cha" });
                    }
                }

                // Kiểm tra Slug 
                bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == categoryDTO.Slug && c.Id != id);
                if (slugExists)
                {
                    return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
                }

                category.Title = categoryDTO.Title.Trim();
                category.Slug = categoryDTO.Slug.Trim().ToLower();
                category.Description = categoryDTO.Description?.Trim();
                category.ParentCategoryId = categoryDTO.ParentCategoryId == -1 ? null : categoryDTO.ParentCategoryId;

                _context.Entry(category).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated: {Title} (ID: {Id})", category.Title, category.Id);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating category with ID {Id}", id);
                return StatusCode(409, new { message = "Dữ liệu đã bị thay đổi bởi người khác" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật danh mục" });
            }
        }

        // DELETE: api/category/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.Categories
                    .Include(c => c.CategoryChildren)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during delete operation", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Di chuyển các danh mục con lên cấp cao hơn
                    foreach (var child in category.CategoryChildren)
                    {
                        child.ParentCategoryId = category.ParentCategoryId;
                    }

                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Category deleted: {Title} (ID: {Id})", category.Title, category.Id);
                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa danh mục" });
            }
        }

        // GET: api/category/{id}/breadcrumb
        [HttpGet("{id}/breadcrumb")]
        public async Task<ActionResult<List<ParentCategoryDTO>>> GetBreadcrumb(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var breadcrumbs = new List<ParentCategoryDTO>();
                var maxDepth = 10; // Giới hạn độ sâu để tránh vòng lặp vô hạn
                var currentDepth = 0;

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during breadcrumb request", id);
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                breadcrumbs.Add(new ParentCategoryDTO
                {
                    Id = category.Id,
                    Title = category.Title,
                    Slug = category.Slug
                });

                while (category.ParentCategoryId.HasValue && currentDepth < maxDepth)
                {
                    category = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId);

                    if (category == null)
                        break;

                    breadcrumbs.Insert(0, new ParentCategoryDTO
                    {
                        Id = category.Id,
                        Title = category.Title,
                        Slug = category.Slug
                    });

                    currentDepth++;
                }

                return Ok(breadcrumbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting breadcrumb for category ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy đường dẫn phân cấp" });
            }
        }

        // Helper method to check if a category is a child of another
        private async Task<bool> IsCategoryChildOf(int parentId, int childId)
        {
            var maxDepth = 10; // Prevent infinite loops
            var currentId = childId;
            var depth = 0;

            while (depth < maxDepth)
            {
                var category = await _context.Categories.FindAsync(currentId);
                if (category == null || !category.ParentCategoryId.HasValue)
                    return false;

                if (category.ParentCategoryId == parentId)
                    return true;

                currentId = category.ParentCategoryId.Value;
                depth++;
            }

            return false;
        }

        // Hàm chuyển đổi Category -> CategoryDTO để tránh vòng lặp
        private CategoryDTO MapToDTO(Category category)
        {
            if (category == null)
                return null;

            return new CategoryDTO
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryTitle = category.ParentCategory?.Title,
                Children = category.CategoryChildren?.Select(MapToDTO).ToList() ?? new List<CategoryDTO>()
            };
        }
    }
}




/*
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
                .Include(c => c.CategoryChildren) // Tải danh mục con cấp 2
                .ThenInclude(c => c.CategoryChildren) // Tải danh mục con cấp 3
                .Include(c => c.ParentCategory)//lấy tên danh mục cha
                .Where(c => c.ParentCategoryId == null) // Lấy danh mục gốc
                .OrderByDescending(c => c.Id)
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
                return BadRequest(new { message = "ID in URL does not match ID in body" });


            if (categoryDTO.ParentCategoryId == id)
                return BadRequest(new { message = "Phải chọn danh mục cha khác." });

            // Kiểm tra Slug 
            bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == categoryDTO.Slug && c.Id != id);
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

        //lấy danh mục theo ID, đồng thời lấy tên và slug danh mục cha và danh mục con nếu có
        [HttpGet("{id}/details")]
        public async Task<ActionResult<CategoryDetailDTO>> GetCategoryWithRelations(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory) // Lấy danh mục cha
                .Include(c => c.CategoryChildren) // Lấy danh mục con
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Danh mục không tồn tại." });

            return new CategoryDetailDTO
            {
                Id = category.Id,
                Title = category.Title,
                Slug = category.Slug,
                Description = category.Description,
                ParentCategory = category.ParentCategory != null
                    ? new ParentCategoryDTO
                    {
                        Title = category.ParentCategory.Title,
                        Slug = category.ParentCategory.Slug
                    }
                    : null,
                Children = category.CategoryChildren?.Select(c => new ChildCategoryDTO
                {
                    Title = c.Title,
                    Slug = c.Slug
                }).ToList()
            };
        }

        [HttpGet("{id}/breadcrumb")]
        public async Task<ActionResult<List<ParentCategoryDTO>>> GetBreadcrumb(int id)
        {
            var breadcrumbs = new List<ParentCategoryDTO>();

            var category = await _context.Categories
                .Include(c => c.ParentCategory) // Include danh mục cha
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Danh mục không tồn tại." });

            while (category != null)
            {
                breadcrumbs.Insert(0, new ParentCategoryDTO
                {
                    Title = category.Title,
                    Slug = category.Slug
                });

                category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId);
            }

            return breadcrumbs;
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

*/