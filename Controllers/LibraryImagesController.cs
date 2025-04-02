using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models;

namespace TravelAPI.Controllers
{
    [Route("api/library-images")]
    [ApiController]
    public class LibraryImagesController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly string[] _validEntityTypes = { "CategoryTour", "TourDetail" };

        public LibraryImagesController(TravelDbContext context)
        {
            _context = context;
        }

        [HttpGet("{entityType}/{entityId}")]
        public async Task<IActionResult> GetImages(string entityType, int entityId)
        {
            if (!_validEntityTypes.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type" });

            var images = await _context.LibraryImages
                .Where(i => i.EntityType == entityType && i.EntityId == entityId)
                .Select(i => new LibraryImageDto { Id = i.Id, ImageUrl = i.ImageUrl })
                .ToListAsync();

            return Ok(images);
        }
        //Thêm một ảnh vào một đối tượng
        [HttpPost("{entityType}/{entityId}/add-image")]
        public async Task<IActionResult> AddImage(string entityType, int entityId, [FromBody] ImageUrlDto dto)
        {
            // Validate entity type
            if (!_validEntityTypes.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type" });

            // Validate model
            if (string.IsNullOrEmpty(dto.ImageUrl))
                return BadRequest(new { message = "Image URL is required" });

            var image = new LibraryImage
            {
                EntityType = entityType,
                EntityId = entityId,
                ImageUrl = dto.ImageUrl
            };

            _context.LibraryImages.Add(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //thêm nhiều ảnh cùng lần
        [HttpPost("{entityType}/{entityId}/add-multiple")]
        public async Task<IActionResult> AddMultipleImages(
            string entityType,
            int entityId,
            [FromBody] List<ImageUrlDto> imageUrls)
        {
            if (!_validEntityTypes.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type" });

            List<LibraryImageDto> addedImages = new List<LibraryImageDto>();

            foreach (var dto in imageUrls)
            {
                var image = new LibraryImage
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ImageUrl = dto.ImageUrl
                };

                _context.LibraryImages.Add(image);
                addedImages.Add(new LibraryImageDto { Id = image.Id, ImageUrl = image.ImageUrl });
            }

            await _context.SaveChangesAsync();

            return Ok(addedImages);
        }
        //Xóa một ảnh theo id ảnh
        [HttpDelete("delete/{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.LibraryImages.FindAsync(imageId);
            if (image == null) return NotFound("Image not found");

            _context.LibraryImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok("Image deleted successfully");
        }

        //Xóa nhiều ảnh 1 lần
        [HttpDelete("delete-multiple")]
        public async Task<IActionResult> DeleteMultipleImages([FromBody] List<int> imageIds)
        {
            int deletedCount = 0;

            foreach (var id in imageIds)
            {
                var image = await _context.LibraryImages.FindAsync(id);
                if (image != null)
                {
                    _context.LibraryImages.Remove(image);
                    deletedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {deletedCount} images successfully" });
        }
        //xóa tất cả ảnh của một đối tượng
        [HttpDelete("{entityType}/{entityId}/delete-all")]
        public async Task<IActionResult> DeleteAllImages(string entityType, int entityId)
        {
            // Validate entity type
            if (!_validEntityTypes.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type" });

            var images = await _context.LibraryImages
                .Where(i => i.EntityType == entityType && i.EntityId == entityId)
                .ToListAsync();

            if (!images.Any())
                return NotFound(new { message = $"No images found for {entityType} with id {entityId}" });

            _context.LibraryImages.RemoveRange(images);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {images.Count} images successfully" });
        }

    }

}