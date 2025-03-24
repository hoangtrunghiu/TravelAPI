using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TravelAPI.Data;

namespace TravelAPI.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public FilesController(TravelDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileDto>>> GetFiles()
        {
            var files = await _context.Files.Select(f => new FileDto
            {
                Id = f.Id,
                Name = f.Name,
                ContentType = f.ContentType,
                Size = f.Size,
                Url = f.Url,
                CreatedAt = f.CreatedAt,
                FolderId = f.FolderId
            }).OrderByDescending(f => f.CreatedAt).ToListAsync();

            return Ok(files);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FileDto>> GetFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            return Ok(new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                Url = file.Url,
                CreatedAt = file.CreatedAt,
                FolderId = file.FolderId
            });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is empty");

            if (request.File.Length > MaxFileSize)
                return BadRequest("File exceeds the maximum allowed size (50MB)");

            // Lấy folderId từ FormData
            int? folderId = null;
            if (Request.Form.ContainsKey("folderId") && int.TryParse(Request.Form["folderId"], out int parsedFolderId))
            {
                folderId = parsedFolderId;
            }

            string uniqueFileName = Path.GetFileNameWithoutExtension(request.File.FileName) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + Path.GetExtension(request.File.FileName);
            string uploadPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
            Directory.CreateDirectory(uploadPath);
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var fileEntity = new FileEntity
            {
                Name = uniqueFileName,
                ContentType = request.File.ContentType,
                Size = request.File.Length,
                Path = filePath,
                Url = $"/uploads/{uniqueFileName}",
                CreatedAt = DateTime.UtcNow,
                FolderId = folderId
            };

            _context.Files.Add(fileEntity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFile), new { id = fileEntity.Id }, fileEntity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutMenu(int id, [FromQuery] int? folderId)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            file.FolderId = folderId;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            if (System.IO.File.Exists(file.Path))
                System.IO.File.Delete(file.Path);

            _context.Files.Remove(file);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null || !System.IO.File.Exists(file.Path))
                return NotFound();

            var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read);
            return File(stream, file.ContentType, file.Name);
        }
        private bool FileExists(int id)
        {
            return _context.Files.Any(e => e.Id == id);
        }
    }
}
