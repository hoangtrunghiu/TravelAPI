using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TravelAPI.Controllers
{
    [Route("api/post")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly ILogger<PostsController> _logger;

        public PostsController(TravelDbContext context, ILogger<PostsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 1 Lấy danh sách tất cả bài viết
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostDTO>>> GetAllPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? published = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                var query = _context.Posts
                    .Include(p => p.MainCategory)
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .AsQueryable();

                // Lọc theo trạng thái published nếu được chỉ định
                if (published.HasValue)
                {
                    query = query.Where(p => p.Published == published.Value);
                }
                // Lọc theo trạng danh mục categoryId(bao gồm cả danh mục con)
                if (categoryId.HasValue)
                {
                    if (categoryId <= 0)
                    {
                        return BadRequest(new { message = "ID danh mục không hợp lệ" });
                    }

                    // Kiểm tra danh mục có tồn tại không
                    var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
                    if (!categoryExists)
                    {
                        return NotFound(new { message = "Danh mục không tồn tại" });
                    }
                    // Lấy toàn bộ ID danh mục con
                    var categoryIds = await _context.Categories
                        .Where(c => c.Id == categoryId || c.ParentCategoryId == categoryId)
                        .Select(c => c.Id)
                        .ToListAsync();
                    query = query.Where(p => p.MainCategoryId != null && categoryIds.Contains(p.MainCategoryId.Value));
                }

                var posts = await query
                    .OrderByDescending(p => p.PostId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PostDTO
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        Slug = p.Slug,
                        Description = p.Description,
                        Content = p.Content,
                        Published = p.Published,
                        DateCreated = p.DateCreated,
                        DateUpdated = p.DateUpdated,
                        MainCategoryId = p.MainCategoryId,
                        MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
                        RelatedCategories = p.PostCategories
                            .Select(pc => new CateDTO
                            {
                                Id = pc.CategoryId,
                                Title = pc.Category.Title
                            }).ToList()
                    })
                    .ToListAsync();

                var totalPosts = await query.CountAsync();

                return Ok(new
                {
                    items = posts,
                    totalCount = totalPosts,
                    currentPage = page,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching posts");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách bài viết" });
            }
        }

        // 2 Lấy chi tiết bài viết theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PostDTO>> GetPostById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var post = await _context.Posts
                    .Include(p => p.MainCategory)
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .Where(p => p.PostId == id)
                    .Select(p => new PostDTO
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        Slug = p.Slug,
                        Description = p.Description,
                        Content = p.Content,
                        Published = p.Published,
                        DateCreated = p.DateCreated,
                        DateUpdated = p.DateUpdated,
                        MainCategoryId = p.MainCategoryId,
                        MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
                        RelatedCategories = p.PostCategories
                            .Select(pc => new CateDTO
                            {
                                Id = pc.CategoryId,
                                Title = pc.Category.Title
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (post == null)
                {
                    _logger.LogWarning("Post with ID {Id} not found", id);
                    return NotFound(new { message = "Bài viết không tồn tại." });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching post with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy chi tiết bài viết" });
            }
        }

        // 3 Lấy danh sách bài viết theo categoryId (bao gồm cả danh mục con)
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<PostDTO>>> GetPostsByCategory(
            int categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? published = null)
        {
            try
            {
                if (categoryId <= 0)
                {
                    return BadRequest(new { message = "ID danh mục không hợp lệ" });
                }

                // Kiểm tra danh mục có tồn tại không
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
                if (!categoryExists)
                {
                    return NotFound(new { message = "Danh mục không tồn tại" });
                }

                // Lấy toàn bộ ID danh mục con
                var categoryIds = await _context.Categories
                    .Where(c => c.Id == categoryId || c.ParentCategoryId == categoryId)
                    .Select(c => c.Id)
                    .ToListAsync();

                var query = _context.Posts
                    .Include(p => p.MainCategory)
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .Where(p => p.MainCategoryId != null && categoryIds.Contains(p.MainCategoryId.Value));

                // Lọc theo trạng thái published nếu được chỉ định
                if (published.HasValue)
                {
                    query = query.Where(p => p.Published == published.Value);
                }

                var posts = await query
                    .OrderByDescending(p => p.PostId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PostDTO
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        Slug = p.Slug,
                        Description = p.Description,
                        Content = p.Content,
                        Published = p.Published,
                        DateCreated = p.DateCreated,
                        DateUpdated = p.DateUpdated,
                        MainCategoryId = p.MainCategoryId,
                        MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
                        RelatedCategories = p.PostCategories
                            .Select(pc => new CateDTO
                            {
                                Id = pc.CategoryId,
                                Title = pc.Category.Title
                            }).ToList()
                    })
                    .ToListAsync();

                var totalPosts = await query.CountAsync();

                return Ok(new
                {
                    Posts = posts,
                    TotalPosts = totalPosts,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching posts for category {CategoryId}", categoryId);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách bài viết theo danh mục" });
            }
        }

        // 4 Thêm bài viết
        [HttpPost]
        public async Task<ActionResult> CreatePost([FromBody] CreatePostDTO postDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Kiểm tra tính hợp lệ của dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(postDto.Title))
                {
                    return BadRequest(new { message = "Tiêu đề bài viết không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(postDto.Slug))
                {
                    return BadRequest(new { message = "Slug không được để trống" });
                }

                // Kiểm tra slug đã tồn tại
                if (await _context.Posts.AnyAsync(p => p.Slug == postDto.Slug))
                {
                    return BadRequest(new { message = "Slug đã tồn tại." });
                }

                // Kiểm tra danh mục chính
                var mainCategoryExists = await _context.Categories.AnyAsync(c => c.Id == postDto.MainCategoryId);
                if (!mainCategoryExists)
                {
                    return BadRequest(new { message = "Danh mục chính không tồn tại." });
                }

                // Kiểm tra danh mục liên quan
                var validCategories = await _context.Categories
                    .Where(c => postDto.RelatedCategoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (validCategories.Count != postDto.RelatedCategoryIds.Count)
                {
                    return BadRequest(new { message = "Một số danh mục liên quan không tồn tại." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                bool committed = false;

                try
                {
                    var post = new Post
                    {
                        Title = postDto.Title.Trim(),
                        Description = postDto.Description?.Trim(),
                        Slug = postDto.Slug.Trim().ToLower(),
                        Content = postDto.Content,
                        Published = postDto.Published,
                        MainCategoryId = postDto.MainCategoryId,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow,
                    };

                    _context.Posts.Add(post);
                    await _context.SaveChangesAsync();

                    var postCategories = validCategories.Select(categoryId => new PostCategory
                    {
                        PostId = post.PostId,
                        CategoryId = categoryId
                    });

                    _context.PostCategories.AddRange(postCategories);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    committed = true;

                    _logger.LogInformation("Post created: {Title} (ID: {Id})", post.Title, post.PostId);
                    return CreatedAtAction(nameof(GetPostById), new { id = post.PostId }, MapToPostDTO(post));
                }
                catch
                {
                    if (!committed)
                    {
                        await transaction.RollbackAsync();
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating post");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo bài viết" });
            }
        }

        // 5 Cập nhật bài viết
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePost(int id, [FromBody] CreatePostDTO postDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var post = await _context.Posts
                    .Include(p => p.PostCategories)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                {
                    _logger.LogWarning("Post with ID {Id} not found during update", id);
                    return NotFound(new { message = "Bài viết không tồn tại." });
                }

                // Kiểm tra slug
                if (string.IsNullOrWhiteSpace(postDto.Slug))
                {
                    return BadRequest(new { message = "Slug không được để trống" });
                }

                if (await _context.Posts.AnyAsync(p => p.Slug == postDto.Slug && p.PostId != id))
                {
                    return BadRequest(new { message = "Slug đã tồn tại." });
                }

                // Kiểm tra danh mục chính
                var mainCategoryExists = await _context.Categories.AnyAsync(c => c.Id == postDto.MainCategoryId);
                if (!mainCategoryExists)
                {
                    return BadRequest(new { message = "Danh mục chính không tồn tại." });
                }

                // Kiểm tra danh mục liên quan
                var validCategories = await _context.Categories
                    .Where(c => postDto.RelatedCategoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (validCategories.Count != postDto.RelatedCategoryIds.Count)
                {
                    return BadRequest(new { message = "Một số danh mục liên quan không tồn tại." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cập nhật thông tin bài viết
                    post.Title = postDto.Title.Trim();
                    post.Description = postDto.Description?.Trim();
                    post.Slug = postDto.Slug.Trim().ToLower();
                    post.Content = postDto.Content;
                    post.Published = postDto.Published;
                    post.MainCategoryId = postDto.MainCategoryId;
                    post.DateUpdated = DateTime.UtcNow;

                    // Xóa các danh mục cũ
                    _context.PostCategories.RemoveRange(post.PostCategories);

                    // Thêm các danh mục mới
                    var postCategories = validCategories.Select(categoryId => new PostCategory
                    {
                        PostId = id,
                        CategoryId = categoryId
                    });

                    _context.PostCategories.AddRange(postCategories);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Post updated: {Title} (ID: {Id})", post.Title, post.PostId);
                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating post with ID {Id}", id);
                return StatusCode(409, new { message = "Dữ liệu đã bị thay đổi bởi người khác" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating post with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật bài viết" });
            }
        }

        // 6 Xóa bài viết (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePost(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var post = await _context.Posts
                    .Include(p => p.PostCategories)
                    .FirstOrDefaultAsync(p => p.PostId == id);

                if (post == null)
                {
                    _logger.LogWarning("Post with ID {Id} not found during delete operation", id);
                    return NotFound(new { message = "Bài viết không tồn tại." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Xóa các liên kết với danh mục
                    _context.PostCategories.RemoveRange(post.PostCategories);

                    // Xóa bài viết
                    _context.Posts.Remove(post);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Post deleted: {Title} (ID: {Id})", post.Title, post.PostId);
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
                _logger.LogError(ex, "Error occurred while deleting post with ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa bài viết" });
            }
        }

        // Hàm chuyển đổi Post sang PostDTO
        private PostDTO MapToPostDTO(Post post)
        {
            if (post == null)
                return null;

            return new PostDTO
            {
                PostId = post.PostId,
                Title = post.Title,
                Slug = post.Slug,
                Description = post.Description,
                Content = post.Content,
                Published = post.Published,
                DateCreated = post.DateCreated,
                DateUpdated = post.DateUpdated,
                MainCategoryId = post.MainCategoryId,
                MainCategoryName = post.MainCategory?.Title,
                RelatedCategories = post.PostCategories?
                    .Where(pc => pc.Category != null)
                    .Select(pc => new CateDTO
                    {
                        Id = pc.CategoryId,
                        Title = pc.Category.Title
                    }).ToList() ?? new List<CateDTO>()
            };
        }
    }
}




// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using TravelAPI.Data;
// using TravelAPI.Models;

// namespace TravelAPI.Controllers{
//     [Route("api/post")]
//     [ApiController]
//     public class PostsController : ControllerBase
//     {
//         private readonly TravelDbContext _context;

//         public PostsController(TravelDbContext context)
//         {
//             _context = context;
//         }

//         // 1 Lấy danh sách tất cả bài viết
//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<PostDTO>>> GetAllPosts()
//         {
//             var posts = await _context.Posts
//                 .Include(p => p.MainCategory)
//                 .Include(p => p.PostCategories)
//                     .ThenInclude(pc => pc.Category)
//                 .Select(p => new PostDTO
//                 {
//                     PostId = p.PostId,
//                     Title = p.Title,
//                     Slug = p.Slug,
//                     Description = p.Description,
//                     Content = p.Content,
//                     Published = p.Published,
//                     DateCreated = p.DateCreated,
//                     DateUpdated = p.DateUpdated,
//                     MainCategoryId = p.MainCategoryId,
//                     MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
//                     RelatedCategories = p.PostCategories
//                         .Select(pc => new CateDTO
//                         {
//                             Id = pc.CategoryId,
//                             Title = pc.Category.Title
//                         }).ToList()
//                 })
//                 .OrderByDescending(p => p.PostId)
//                 .ToListAsync();

//             return Ok(posts);
//         }

//         // 2 Lấy chi tiết bài viết theo ID
//         [HttpGet("{id}")]
//         public async Task<ActionResult<PostDTO>> GetPostById(int id)
//         {
//             var post = await _context.Posts
//                 .Include(p => p.MainCategory)
//                 .Include(p => p.PostCategories)
//                     .ThenInclude(pc => pc.Category)
//                 .Where(p => p.PostId == id)
//                 .Select(p => new PostDTO
//                 {
//                     PostId = p.PostId,
//                     Title = p.Title,
//                     Slug = p.Slug,
//                     Description = p.Description,
//                     Content = p.Content,
//                     Published = p.Published,
//                     DateCreated = p.DateCreated,
//                     DateUpdated = p.DateUpdated,
//                     MainCategoryId = p.MainCategoryId,
//                     MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
//                     RelatedCategories = p.PostCategories
//                         .Select(pc => new CateDTO
//                         {
//                             Id = pc.CategoryId,
//                             Title = pc.Category.Title
//                         }).ToList()
//                 })
//                 .FirstOrDefaultAsync();

//             if (post == null) return NotFound(new { message = "Bài viết không tồn tại." });
//             return Ok(post);
//         }

//         // 3 Lấy danh sách bài viết theo categoryId (bao gồm cả danh mục con)
//         [HttpGet("category/{categoryId}")]
//         public async Task<ActionResult<IEnumerable<PostDTO>>> GetPostsByCategory(int categoryId)
//         {
//             var categoryIds = await _context.Categories
//                 .Where(c => c.Id == categoryId || c.ParentCategoryId == categoryId)
//                 .Select(c => c.Id)
//                 .ToListAsync();

//             var posts = await _context.Posts
//                 .Include(p => p.MainCategory)
//                 .Include(p => p.PostCategories)
//                     .ThenInclude(pc => pc.Category)
//                 .Where(p => p.MainCategoryId != null && categoryIds.Contains(p.MainCategoryId.Value))
//                 .Select(p => new PostDTO
//                 {
//                     PostId = p.PostId,
//                     Title = p.Title,
//                     Slug = p.Slug,
//                     Description = p.Description,
//                     Content = p.Content,
//                     Published = p.Published,
//                     DateCreated = p.DateCreated,
//                     DateUpdated = p.DateUpdated,
//                     MainCategoryId = p.MainCategoryId,
//                     MainCategoryName = p.MainCategory != null ? p.MainCategory.Title : null,
//                     RelatedCategories = p.PostCategories
//                         .Select(pc => new CateDTO
//                         {
//                             Id = pc.CategoryId,
//                             Title = pc.Category.Title
//                         }).ToList()
//                 })
//                 .ToListAsync();

//             return Ok(posts);
//         }

//         // 4 Thêm bài viết
//         [HttpPost]
//         public async Task<ActionResult> CreatePost(CreatePostDTO postDto)
//         {
//             try
//             {
//                 if (await _context.Posts.AnyAsync(p => p.Slug == postDto.Slug))
//                     return BadRequest(new {message = "Slug đã tồn tại." });

//                 var mainCategoryExists = await _context.Categories.AnyAsync(c => c.Id == postDto.MainCategoryId);
//                 if (!mainCategoryExists)
//                     return BadRequest(new {message = "Danh mục chính không tồn tại." });

//                 var post = new Post
//                 {
//                     Title = postDto.Title,
//                     Description = postDto.Description,
//                     Slug = postDto.Slug,
//                     Content = postDto.Content,
//                     Published = postDto.Published,
//                     MainCategoryId = postDto.MainCategoryId,
//                     DateCreated = DateTime.UtcNow,
//                     DateUpdated = DateTime.UtcNow,
//                 };

//                 _context.Posts.Add(post);
//                 await _context.SaveChangesAsync();

//                 var validCategories = await _context.Categories
//                     .Where(c => postDto.RelatedCategoryIds.Contains(c.Id))
//                     .Select(c => c.Id)
//                     .ToListAsync();

//                 foreach (var categoryId in validCategories)
//                 {
//                     _context.PostCategories.Add(new PostCategory
//                     {
//                         PostId = post.PostId,
//                         CategoryId = categoryId
//                     });
//                 }

//                 await _context.SaveChangesAsync();
//                 return CreatedAtAction(nameof(GetPostById), new { id = post.PostId }, post);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, $"Lỗi khi tạo bài viết: {ex.Message}");
//             }
//         }



//         // 5 Cập nhật bài viết
//         [HttpPut("{id}")]
//         public async Task<ActionResult> UpdatePost(int id, CreatePostDTO postDto)
//         {
//             var post = await _context.Posts.FindAsync(id);
//             if (post == null) return NotFound(new {message= "Bài viết không tồn tại." });

//             if (await _context.Posts.AnyAsync(p => p.Slug == postDto.Slug && p.PostId != id))
//             {
//                     return BadRequest(new { message = "Slug đã tồn tại." });
//             }

//             post.Title = postDto.Title;
//             post.Description = postDto.Description;
//             post.Slug = postDto.Slug;
//             post.Content = postDto.Content;
//             post.Published = postDto.Published;
//             post.MainCategoryId = postDto.MainCategoryId;
//             post.DateUpdated = DateTime.UtcNow;

//             _context.PostCategories.RemoveRange(_context.PostCategories.Where(pc => pc.PostId == id));

//             foreach (var categoryId in postDto.RelatedCategoryIds)
//             {
//                 _context.PostCategories.Add(new PostCategory
//                 {
//                     PostId = id,
//                     CategoryId = categoryId
//                 });
//             }

//             await _context.SaveChangesAsync();
//             return NoContent();
//         }
//     //    [HttpDelete]
//     //    public async Task<ActionResult> DeletePost(int id)
//     //     {
//     //         var post = await _context.Posts.FindAsync(id);
//     //         if (post == null) return NotFound("Bài viết không tồn tại.");
//     //         post.IsDelete = true;
//     //         await _context.SaveChangesAsync();
//     //         return NoContent();
//     //     }
//     }


// }