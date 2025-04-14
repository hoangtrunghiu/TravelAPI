using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models.Tour;

namespace TravelAPI.Controllers.Tour{
    [ApiController]
    [Route("api/tour-date")]
    public class TourDatesController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public TourDatesController(TravelDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách ngày khởi hành của một tour
        [HttpGet("{tourId}")]
        public async Task<ActionResult<IEnumerable<TourDate>>> GetTourDatesByTourId(int tourId)
        {
            var tourDates = await _context.TourDates
                .Where(td => td.TourDetailId == tourId)
                .OrderBy(td => td.StartDate)
                .ToListAsync();

            return tourDates;
        }

        // Thêm nhiều ngày khởi hành cho một tour
        [HttpPost]
        public async Task<ActionResult<IEnumerable<TourDate>>> AddTourDates([FromBody] AddTourDatesRequest request)
        {
            // Kiểm tra tour tồn tại
            var tourExists = await _context.TourDetails.AnyAsync(t => t.Id == request.TourId);
            if (!tourExists)
            {
                return NotFound($"Không tìm thấy tour với ID: {request.TourId}");
            }

            var tourDates = request.StartDates.Select(date => new TourDate
            {
                TourDetailId = request.TourId,
                StartDate = DateTime.Parse(date)
            }).ToList();

            _context.TourDates.AddRange(tourDates);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTourDatesByTourId), new { tourId = request.TourId }, tourDates);
        }

        // Xóa tất cả ngày khởi hành của một tour
        [HttpDelete("{tourId}")]
        public async Task<IActionResult> DeleteTourDates(int tourId)
        {
            var tourDates = await _context.TourDates
                .Where(td => td.TourDetailId == tourId)
                .ToListAsync();

            if (tourDates.Any())
            {
                _context.TourDates.RemoveRange(tourDates);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }

    // Lớp request để nhận dữ liệu từ client
    public class AddTourDatesRequest
    {
        public int TourId { get; set; }
        public List<string> StartDates { get; set; } = new List<string>();
    }

}