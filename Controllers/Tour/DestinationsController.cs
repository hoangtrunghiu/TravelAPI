using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Models.Tour;
using TravelAPI.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TravelAPI.Controllers.Tour
{
    [Route("api/destination")]
    [ApiController]
    // [Authorize(Roles = "Administrator")]
    public class DestinationsController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public DestinationsController(TravelDbContext context)
        {
            _context = context;
        }

        // GET: api/destination
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DestinationDTO>>> GetDestinations()
        {
            var destinations = await _context.Destinations
                .Include(c => c.DestinationChildren) // Tải mục con cấp 2
                .Include(c => c.ParentDestination)//lấy tên mục cha
                .Where(c => c.ParentId == null) // Lấy mục gốc
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return destinations.Select(MapToDTO).ToList();
        }

        // GET: api/destination/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DestinationDTO>> GetDestination(int id)
        {
            var destination = await _context.Destinations
                .Include(c => c.DestinationChildren)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (destination == null)
                return NotFound();

            return MapToDTO(destination);
        }

        // POST: api/destination
        [HttpPost]
        public async Task<ActionResult<DestinationDTO>> CreateDestination(DestinationDTO DestinationDTO)
        {
            if (DestinationDTO.ParentId == -1)
                DestinationDTO.ParentId = null;

            var destination = new Destination
            {
                Name = DestinationDTO.Name,
                ParentId = DestinationDTO.ParentId
            };

            _context.Destinations.Add(destination);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDestination), new { id = destination.Id }, MapToDTO(destination));
        }

        // PUT: api/destination/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDestination(int id, DestinationDTO DestinationDTO)
        {
            if (id != DestinationDTO.Id)
                return BadRequest(new { message = "ID in URL does not match ID in body" });


            if (DestinationDTO.ParentId == id)
                return BadRequest(new { message = "Phải chọn điểm đến cha khác." });


            if(DestinationDTO.ParentId.HasValue)
            {
                if (await IsDestinationChildOf(id, DestinationDTO.ParentId.Value))
                {
                    return BadRequest(new { message = "Không thể chọn một điểm đến con làm cha" });
                }
            }
            var destination = await _context.Destinations.FindAsync(id);
            if (destination == null)
                return NotFound();

            destination.Name = DestinationDTO.Name;
            destination.ParentId = DestinationDTO.ParentId == -1 ? null : DestinationDTO.ParentId;

            _context.Destinations.Update(destination);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/destination/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDestination(int id)
        {
            var destination = await _context.Destinations
                .Include(c => c.DestinationChildren)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (destination == null)
                return NotFound();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Di chuyển các danh mục con lên cấp cao hơn
                foreach (var child in destination.DestinationChildren)
                    child.ParentId = destination.ParentId;

                // Kiểm tra phụ thuộc với TourDestinations
                bool hasTourDestination = await _context.TourDestinations.AnyAsync(td => td.DestinationId == id);
                // Xử lý các TourDestinations liên quan
                if (hasTourDestination)
                {
                    var tourDestination = await _context.TourDestinations
                        .Where(pc => pc.DestinationId == id)
                        .ToListAsync();
                    // Xóa các liên kết TourDestinations
                    _context.TourDestinations.RemoveRange(tourDestination);
                }

                // Xóa destination
                _context.Remove(destination);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        private async Task<bool> IsDestinationChildOf(int parentId, int childId)
        {
            var maxDepth = 10; // Prevent infinite loops
            var currentId = childId;
            var depth = 0;

            while (depth < maxDepth)
            {
                var destination = await _context.Destinations.FindAsync(currentId);
                if (destination == null || !destination.ParentId.HasValue)
                    return false;

                if (destination.ParentId == parentId)
                    return true;

                currentId = destination.ParentId.Value;
                depth++;
            }

            return false;
        }

        // Hàm chuyển đổi Destination -> DestinationDTO để tránh vòng lặp
        private DestinationDTO MapToDTO(Destination Destination)
        {
            return new DestinationDTO
            {
                Id = Destination.Id,
                Name = Destination.Name,
                ParentId = Destination.ParentId,
                ParentName = Destination.ParentDestination?.Name,
                Children = Destination.DestinationChildren.Select(MapToDTO).ToList()
            };
        }
    }
}