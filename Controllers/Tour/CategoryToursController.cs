using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models.Tour;
using TravelAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace TravelAPI.Controllers.Tour
{
    [Route("api/category-tour")]
    [ApiController]
    // [Authorize(Roles = "Administrator,Editor")]
    public class CategoryToursController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly ILogger<CategoryToursController> _logger;

        public CategoryToursController(TravelDbContext context, ILogger<CategoryToursController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryTourDTO>>> GetCategoryTours()
        {
            try
            {
                var categoryTour = await _context.CategoryTours
                    .Include(c => c.CategoryTourChildren) // Tải danh mục con cấp 2
                    .ThenInclude(c => c.CategoryTourChildren) // Tải danh mục con cấp 3
                    .Include(c => c.ParentCategoryTour) // Lấy thêm danh mục cha => lấy ra tên trong dto
                    .Where(c => c.ParentCategoryTourId == null && !c.IsDeleted) // Lấy danh mục gốc và chưa bị xóa
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                return Ok(categoryTour.Select(MapToDTO).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category tours");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách danh mục tour" });
            }
        }

        // GET: api/category/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryTourDTO>> GetCategoryTour(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.CategoryTours
                   .Include(c => c.CategoryTourChildren.Where(child => !child.IsDeleted)) // Chỉ lấy danh mục con chưa bị xóa
                   .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục tour với ID: {id}" });
                }

                return Ok(MapToDTO(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin danh mục tour" });
            }
        }

        // POST: api/category
        [HttpPost]
        public async Task<ActionResult<CategoryTourDTO>> CreateCategory([FromBody] CategoryTourDTO categoryTourDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(categoryTourDTO.Url))
                {
                    return BadRequest(new { message = "Đường dẫn URL không được để trống" });
                }

                if (categoryTourDTO.ParentCategoryTourId == -1)
                {
                    categoryTourDTO.ParentCategoryTourId = null;
                }
                else if (categoryTourDTO.ParentCategoryTourId.HasValue)
                {
                    // Kiểm tra xem danh mục cha có tồn tại không
                    var parentExists = await _context.CategoryTours.AnyAsync(c => c.Id == categoryTourDTO.ParentCategoryTourId);
                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Danh mục cha không tồn tại" });
                    }
                }

                // Kiểm tra nếu URL đã tồn tại
                bool urlExists = await _context.CategoryTours.AnyAsync(c => c.Url == categoryTourDTO.Url);
                if (urlExists)
                {
                    return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
                }

                var category = new CategoryTour
                {
                    CategoryName = categoryTourDTO.CategoryName.Trim(),
                    Topic = categoryTourDTO.Topic.Trim(),
                    Url = categoryTourDTO.Url.Trim().ToLower(),
                    Description = categoryTourDTO.Description?.Trim(),
                    ContentIntro = categoryTourDTO.ContentIntro,
                    ContentDetail = categoryTourDTO.ContentDetail,
                    Avatar = string.IsNullOrWhiteSpace(categoryTourDTO.Avatar) ? "default-image.png" : categoryTourDTO.Avatar,
                    Creator = categoryTourDTO.Creator,
                    CreatorName = categoryTourDTO.CreatorName,
                    // Editor = categoryTourDTO.Editor,
                    // EditorName = categoryTourDTO.EditorName,
                    MetaTitle = categoryTourDTO.MetaTitle,
                    MetaDescription = categoryTourDTO.MetaDescription,
                    MetaKeywords = categoryTourDTO.MetaKeywords,
                    IsIndexRobot = categoryTourDTO.IsIndexRobot,
                    ParentCategoryTourId = categoryTourDTO.ParentCategoryTourId
                };

                _context.CategoryTours.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created: {Title} (ID: {Id})", category.CategoryName, category.Id);
                return CreatedAtAction(nameof(GetCategoryTour), new { id = category.Id }, MapToDTO(category));
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
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryTourDTO categoryTourDTO)
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

                if (id != categoryTourDTO.Id)
                {
                    return BadRequest(new { message = "ID trong URL không khớp với ID trong dữ liệu" });
                }

                if (string.IsNullOrWhiteSpace(categoryTourDTO.Url))
                {
                    return BadRequest(new { message = "URL không được để trống" });
                }

                // Kiểm tra xem danh mục có tồn tại không
                var category = await _context.CategoryTours.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during update", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                // Kiểm tra nếu danh mục cha là chính nó
                if (categoryTourDTO.ParentCategoryTourId == id)
                {
                    return BadRequest(new { message = "Danh mục không thể là cha của chính nó" });
                }

                // Kiểm tra xem danh mục cha có tồn tại không
                if (categoryTourDTO.ParentCategoryTourId.HasValue && categoryTourDTO.ParentCategoryTourId != -1)
                {
                    bool parentExists = await _context.CategoryTours.AnyAsync(c => c.Id == categoryTourDTO.ParentCategoryTourId);
                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Danh mục cha không tồn tại" });
                    }

                    // Kiểm tra nếu danh mục cha là con của danh mục hiện tại (tránh vòng lặp)
                    if (await IsCategoryChildOf(id, categoryTourDTO.ParentCategoryTourId.Value))
                    {
                        return BadRequest(new { message = "Không thể chọn một danh mục con làm cha" });
                    }
                }

                // Kiểm tra Url 
                bool urlExists = await _context.CategoryTours.AnyAsync(c => c.Url == categoryTourDTO.Url && c.Id != id);
                if (urlExists)
                {
                    return BadRequest(new { message = "URL đã tồn tại, hãy tạo URL khác" });
                }

                category.CategoryName = categoryTourDTO.CategoryName.Trim();
                category.Topic = categoryTourDTO.Topic.Trim();
                category.Url = categoryTourDTO.Url.Trim().ToLower();
                category.Description = categoryTourDTO.Description?.Trim();
                category.ContentIntro = categoryTourDTO.ContentIntro;
                category.ContentDetail = categoryTourDTO.ContentDetail;
                category.Avatar = categoryTourDTO.Avatar;
                category.Editor = categoryTourDTO.Editor;
                category.EditorName = categoryTourDTO.EditorName;
                category.MetaTitle = categoryTourDTO.MetaTitle;
                category.MetaDescription = categoryTourDTO.MetaDescription;
                category.MetaKeywords = categoryTourDTO.MetaKeywords;
                category.IsIndexRobot = categoryTourDTO.IsIndexRobot;
                category.ParentCategoryTourId = categoryTourDTO.ParentCategoryTourId == -1 ? null : categoryTourDTO.ParentCategoryTourId;

                _context.Entry(category).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated: {Title} (ID: {Id})", category.CategoryName, category.Id);
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
        public async Task<IActionResult> DeleteCategoryTour(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.CategoryTours
                    .Include(c => c.CategoryTourChildren)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during delete operation", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                // Kiểm tra phụ thuộc với TourDetail (MainCategoryTour)
                bool isMainCategory = await _context.TourDetails.AnyAsync(td => td.MainCategoryTourId == id);

                // Kiểm tra phụ thuộc với TourCategoryMapping
                bool hasTourCategoryMapping = await _context.TourCategoryMappings.AnyAsync(tc => tc.CategoryTourId == id);

                //Kiểm tra sự phụ thuộc với Image - sẽ thêm sau

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Xóa mềm danh mục
                    category.IsDeleted = true;

                    // Xử lý các danh mục con
                    foreach (var child in category.CategoryTourChildren)
                    {
                        // Chuyển danh mục con lên cấp cao hơn
                        child.ParentCategoryTourId = category.ParentCategoryTourId;
                    }

                    // Xử lý các TourDetail có MainCategoryTourId là category này
                    if (isMainCategory)
                    {
                        var relatedTourDetails = await _context.TourDetails
                            .Where(td => td.MainCategoryTourId == id)
                            .ToListAsync();

                        foreach (var tourDetail in relatedTourDetails)
                        {
                            // Xóa liên kết MainCategory
                            tourDetail.MainCategoryTourId = null;
                        }
                    }

                    // Xử lý các TourCategoryMapping liên quan
                    if (hasTourCategoryMapping)
                    {
                        var tourCategoryMapping = await _context.TourCategoryMappings
                            .Where(tc => tc.CategoryTourId == id)
                            .ToListAsync();

                        // Xóa các liên kết TourCategoryMapping
                        _context.TourCategoryMappings.RemoveRange(tourCategoryMapping);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Category soft deleted: {Title} (ID: {Id})", category.CategoryName, category.Id);
                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed during category deletion");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa danh mục" });
            }
        }


        // Thêm endpoint mới để khôi phục danh mục đã xóa mềm
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.CategoryTours
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);

                if (category == null)
                {
                    _logger.LogWarning("Deleted category with ID {Id} not found during restore operation", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục đã xóa với ID: {id}" });
                }

                // Khôi phục danh mục
                category.IsDeleted = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category restored: {Title} (ID: {Id})", category.CategoryName, category.Id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while restoring category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi khôi phục danh mục" });
            }
        }

        // GET: api/category/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<IEnumerable<CategoryTourDTO>>> GetDeletedCategoryTours()
        {
            try
            {
                var deletedCategoryTours = await _context.CategoryTours
                    .Where(c => c.IsDeleted)
                    .ToListAsync();

                return Ok(deletedCategoryTours.Select(MapToDTO).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting deleted category tours");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách danh mục đã xóa" });
            }
        }

        // endpoint để xóa vĩnh viễn danh mục
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Administrator")] // Chỉ admin mới có quyền xóa vĩnh viễn
        public async Task<IActionResult> PermanentDeleteCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var category = await _context.CategoryTours
                    .Include(c => c.CategoryTourChildren)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning("Category tour with ID {Id} not found during permanent delete operation", id);
                    return NotFound(new { message = $"Không tìm thấy danh mục với ID: {id}" });
                }

                // Kiểm tra phụ thuộc với TourDetail (MainCategoryTour)
                bool isMainCategory = await _context.TourDetails.AnyAsync(td => td.MainCategoryTourId == id);

                // Kiểm tra phụ thuộc với TourCategoryMapping
                bool hasTourCategoryMapping = await _context.TourCategoryMappings.AnyAsync(pc => pc.CategoryTourId == id);

                if (isMainCategory || hasTourCategoryMapping)
                {
                    return BadRequest(new { message = "Không thể xóa vĩnh viễn danh mục này vì vẫn còn liên kết với các bài viết" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Di chuyển các danh mục con lên cấp cao hơn
                    foreach (var child in category.CategoryTourChildren)
                    {
                        child.ParentCategoryTourId = category.ParentCategoryTourId;
                    }

                    _context.CategoryTours.Remove(category);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Category permanently deleted: {Title} (ID: {Id})", category.CategoryName, category.Id);
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
                _logger.LogError(ex, "Error occurred while permanently deleting category with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa vĩnh viễn danh mục" });
            }
        }

        // GET: api/category/{id}/breadcrumb
        [HttpGet("{id}/breadcrumb")]
        public async Task<ActionResult<List<ParentCategoryTourDTO>>> GetBreadcrumb(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var breadcrumbs = new List<ParentCategoryTourDTO>();
                var maxDepth = 10; // Giới hạn độ sâu để tránh vòng lặp vô hạn
                var currentDepth = 0;

                var category = await _context.CategoryTours
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {Id} not found during breadcrumb request", id);
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                breadcrumbs.Add(new ParentCategoryTourDTO
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                    Url = category.Url
                });

                while (category.ParentCategoryTourId.HasValue && currentDepth < maxDepth)
                {
                    category = await _context.CategoryTours
                        .FirstOrDefaultAsync(c => c.Id == category.ParentCategoryTourId);

                    if (category == null)
                        break;

                    breadcrumbs.Insert(0, new ParentCategoryTourDTO
                    {
                        Id = category.Id,
                        CategoryName = category.CategoryName,
                        Url = category.Url
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
                var category = await _context.CategoryTours.FindAsync(currentId);
                if (category == null || !category.ParentCategoryTourId.HasValue)
                    return false;

                if (category.ParentCategoryTourId == parentId)
                    return true;

                currentId = category.ParentCategoryTourId.Value;
                depth++;
            }

            return false;
        }

        // Hàm chuyển đổi CategoryTour -> CategoryTourDTO để tránh vòng lặp
        private CategoryTourDTO MapToDTO(CategoryTour categoryTour)
        {
            if (categoryTour == null)
                return null;

            return new CategoryTourDTO
            {
                Id = categoryTour.Id,
                CategoryName = categoryTour.CategoryName,
                Topic = categoryTour.Topic,
                Url = categoryTour.Url,
                Description = categoryTour.Description,
                ContentIntro = categoryTour.ContentIntro,
                ContentDetail = categoryTour.ContentDetail,
                Avatar = categoryTour.Avatar,
                Creator = categoryTour.Creator,
                CreatorName = categoryTour.CreatorName,
                Editor = categoryTour.Editor,
                EditorName = categoryTour.EditorName,
                MetaTitle = categoryTour.MetaTitle,
                MetaDescription = categoryTour.MetaDescription,
                MetaKeywords = categoryTour.MetaKeywords,
                IsIndexRobot = categoryTour.IsIndexRobot,
                ParentCategoryTourId = categoryTour.ParentCategoryTourId,
                ParentCategoryTourName = categoryTour.ParentCategoryTour?.CategoryName,
                IsDeleted = categoryTour.IsDeleted,
                Children = categoryTour.CategoryTourChildren?
                    .Where(c => !c.IsDeleted)
                    .Select(MapToDTO)
                    .ToList() ?? new List<CategoryTourDTO>()
            };
        }
    }
}


