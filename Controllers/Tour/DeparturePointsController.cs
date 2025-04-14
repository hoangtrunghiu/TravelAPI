using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models.Tour;

namespace TravelAPI.Controllers.Tour
{
    [Route("api/departure-point")]
    [ApiController]
    public class DeparturePointsController : ControllerBase
    {
        private readonly TravelDbContext _context;

        public DeparturePointsController(TravelDbContext context)
        {
            _context = context;
        }

        // GET: api/departure-point
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeparturePoint>>> GetDeparturePoints()
        {
            return await _context.DeparturePoints.ToListAsync();
        }

        // GET: api/departure-point/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DeparturePoint>> GetDeparturePoint(int id)
        {
            var DeparturePoint = await _context.DeparturePoints.FindAsync(id);

            if (DeparturePoint == null)
                return NotFound();

            return DeparturePoint;
        }

        // POST: api/departure-point
        [HttpPost]
        public async Task<ActionResult<DeparturePoint>> PostDeparturePoint(DeparturePoint departurePoint)
        {
            _context.DeparturePoints.Add(departurePoint);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDeparturePoint), new { id = departurePoint.Id }, departurePoint);
        }

        // PUT: api/departure-point/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeparturePoint(int id, DeparturePoint departurePoint)
        {
            if (id != departurePoint.Id)
            {
                return BadRequest(new { message = "ID in URL does not match ID in body" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(departurePoint).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeparturePointExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/departure-point/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeparturePoint(int id)
        {
            var departurePoint = await _context.DeparturePoints.FindAsync(id);
            if (departurePoint == null) return NotFound();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra phụ thuộc với TourDeparture
                bool hasTourDeparture = await _context.TourDepartures.AnyAsync(td => td.DeparturePointId == id);
                // Xử lý các TourDeparture liên quan
                if (hasTourDeparture)
                {
                    var tourDeparture = await _context.TourDepartures
                        .Where(pc => pc.DeparturePointId == id)
                        .ToListAsync();
                    // Xóa các liên kết TourDepartures
                    _context.TourDepartures.RemoveRange(tourDeparture);
                }

                // Xóa departurePoint
                _context.Remove(departurePoint);
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

        private bool DeparturePointExists(int id)
        {
            return _context.DeparturePoints.Any(e => e.Id == id);
        }
    }
}
