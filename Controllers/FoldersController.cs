using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models.Files;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TravelAPI.Controllers
{
    [Route("api/folders")]
    [ApiController]
    public class FoldersController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public FoldersController(TravelDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả thư mục
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolderDto>>> GetAllFolders()
        {
            var folders = await _context.Folders
                .Select(f => new FolderDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(folders);
        }

        // Lấy thông tin thư mục theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<FolderDto>> GetFolderById(int id)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
                return NotFound();

            return Ok(new FolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                CreatedAt = folder.CreatedAt
            });
        }

        // Lấy danh sách file trong thư mục
        [HttpGet("{id}/files")]
        public async Task<ActionResult<IEnumerable<FileDto>>> GetFilesInFolder(int id)
        {
            var files = await _context.Files
                .Where(f => f.FolderId == id)
                .Select(f => new FileDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    Size = f.Size,
                    Url = f.Url,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId
                })
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(files);
        }

        // Tạo thư mục mới
        [HttpPost]
        public async Task<ActionResult<FolderDto>> CreateFolder(CreateFolderDto request)
        {
            var folder = new Folder
            {
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFolderById), new { id = folder.Id }, new FolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                CreatedAt = folder.CreatedAt
            });
        }

        // Xóa thư mục (chỉ khi trống)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
                return NotFound();

            // Kiểm tra thư mục có chứa file không
            if (folder.Files.Any())
                return BadRequest(new { message = "Thư mục còn chứa tệp. Xóa hoặc chuyển tất cả tệp trước khi xóa thư mục" });

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
