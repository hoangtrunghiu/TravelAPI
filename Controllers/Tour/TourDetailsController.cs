using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelAPI.Data;
using TravelAPI.Models.Tour;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace TravelAPI.Controllers
{
    [Route("api/tourdetails")]
    [ApiController]
    public class TourDetailsController : ControllerBase
    {
        private readonly TravelDbContext _context;
        private readonly ILogger<TourDetailsController> _logger;

        public TourDetailsController(TravelDbContext context, ILogger<TourDetailsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 1. Lấy danh sách tất cả tour
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TourDetailDTO>>> GetAllTours(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isHot = null,
            [FromQuery] bool? isHide = null,
            [FromQuery] int? categoryTourId = null)
        {
            try
            {
                var query = _context.TourDetails
                    .Include(t => t.MainCategoryTour)
                    .Include(t => t.TourCategoryMappings)
                        .ThenInclude(tcm => tcm.CategoryTour)
                    .Where(t => !t.IsDelete) // Chỉ lấy các tour chưa bị xóa    
                    .AsQueryable();

                // Lọc theo isHot nếu được chỉ định
                if (isHot.HasValue)
                {
                    query = query.Where(t => t.IsHot == isHot.Value);
                }
                // Lọc theo isHide nếu được chỉ định
                if (isHide.HasValue)
                {
                    query = query.Where(t => t.IsHide == isHide.Value);
                }

                // Lọc theo danh mục chính bao gồm cả danh mục con
                if (categoryTourId.HasValue)
                {
                    if (categoryTourId <= 0)
                    {
                        return BadRequest(new { message = "ID danh mục không hợp lệ" });
                    }

                    // Kiểm tra danh mục có tồn tại không
                    var categoryExists = await _context.CategoryTours.AnyAsync(c => c.Id == categoryTourId);
                    if (!categoryExists)
                    {
                        return NotFound(new { message = "Danh mục không tồn tại" });
                    }
                    // Lấy toàn bộ ID danh mục con
                    var categoryIds = await _context.CategoryTours
                        .Where(c => c.Id == categoryTourId || c.ParentCategoryTourId == categoryTourId)
                        .Select(c => c.Id)
                        .ToListAsync();
                    query = query.Where(p => p.MainCategoryTourId != null && categoryIds.Contains(p.MainCategoryTourId.Value));
                }

                var tours = await query
                    .OrderByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TourDetailDTO
                    {
                        Id = t.Id,
                        CodeTour = t.CodeTour,
                        NameTour = t.NameTour,
                        //OriginalPrice = t.OriginalPrice,
                        //PromotionallPrice = t.PromotionallPrice,
                        CountryFrom = t.CountryFrom,
                        CountryTo = t.CountryTo,
                        Hotel = t.Hotel,
                        Flight = t.Flight,
                        //Notes = t.Notes,
                        //Timeline = t.Timeline,
                        //Description = t.Description,
                        Url = t.Url,
                        //Promotion = t.Promotion,
                        Avatar = t.Avatar,
                        CreateAt = t.CreateAt,
                        Creater = t.Creater,
                        IsHot = t.IsHot,
                        IsHide = t.IsHide,
                        MainCategoryTourId = t.MainCategoryTourId,
                        MainCategoryTourName = t.MainCategoryTour != null ? t.MainCategoryTour.CategoryName : null,
                        // RelatedCategories = t.TourCategoryMappings
                        //     .Select(tcm => new ChildTourDTO
                        //     {
                        //         Id = tcm.CategoryTourId,
                        //         Name = tcm.CategoryTour.CategoryName
                        //     }).ToList(),
                        // RelatedDestinations = t.TourDestinations
                        //     .Select(td => new ChildTourDTO
                        //     {
                        //         Id = td.DestinationId,
                        //         Name = td.Destination.Name
                        //     }).ToList(),
                        // RelatedDeparturePoints = t.TourDepartures
                        //     .Select(td => new ChildTourDTO
                        //     {
                        //         Id = td.DeparturePointId,
                        //         Name = td.DeparturePoint.Name
                        //     }).ToList(),
                        // RelatedTourDates = t.TourDates
                        //     .Select(td => new TourDateDTO
                        //     {
                        //         Id = td.Id,
                        //         StartDate = td.StartDate
                        //     }).ToList()
                    })
                    .ToListAsync();

                var totalTours = await query.CountAsync();

                return Ok(new
                {
                    items = tours,
                    totalCount = totalTours,
                    currentPage = page,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi lấy danh sách tour");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách tour" });
            }
        }

        // 2. Lấy chi tiết tour theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<TourDetailDTO>> GetTourById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var tour = await _context.TourDetails
                    .Include(t => t.MainCategoryTour)
                    .Include(t => t.TourCategoryMappings)
                        .ThenInclude(tcm => tcm.CategoryTour)
                    .Include(t => t.TourDestinations)
                    .Include(t => t.TourDepartures)
                    .Include(t => t.TourDates)
                    .Where(t => t.Id == id && !t.IsDelete) // Chỉ lấy tour chưa bị xóa
                    .Select(t => new TourDetailDTO
                    {
                        Id = t.Id,
                        CodeTour = t.CodeTour,
                        NameTour = t.NameTour,
                        OriginalPrice = t.OriginalPrice,
                        PromotionallPrice = t.PromotionallPrice,
                        CountryFrom = t.CountryFrom,
                        CountryTo = t.CountryTo,
                        Hotel = t.Hotel,
                        Flight = t.Flight,
                        Notes = t.Notes,
                        Timeline = t.Timeline,
                        Description = t.Description,
                        Url = t.Url,
                        Promotion = t.Promotion,
                        Avatar = t.Avatar,
                        CreateAt = t.CreateAt,
                        Creater = t.Creater,
                        IsHot = t.IsHot,
                        IsHide = t.IsHide,
                        MainCategoryTourId = t.MainCategoryTourId,
                        MainCategoryTourName = t.MainCategoryTour != null ? t.MainCategoryTour.CategoryName : null,
                        RelatedCategories = t.TourCategoryMappings
                            .Select(tcm => new ChildTourDTO
                            {
                                Id = tcm.CategoryTourId,
                                Name = tcm.CategoryTour.CategoryName
                            }).ToList(),
                        RelatedDestinations = t.TourDestinations
                            .Select(td => new ChildTourDTO
                            {
                                Id = td.DestinationId,
                                Name = td.Destination.Name
                            }).ToList(),
                        RelatedDeparturePoints = t.TourDepartures
                            .Select(td => new ChildTourDTO
                            {
                                Id = td.DeparturePointId,
                                Name = td.DeparturePoint.Name
                            }).ToList(),
                        RelatedTourDates = t.TourDates
                            .Select(td => new TourDateDTO
                            {
                                Id = td.Id,
                                StartDate = td.StartDate
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                return Ok(tour);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi lấy chi tiết tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy chi tiết tour" });
            }
        }

        // 3. Thêm tour mới
        [HttpPost]
        public async Task<ActionResult<TourDetailDTO>> CreateTour([FromBody] CreateTourDetailDTO tourDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Kiểm tra tính hợp lệ của dữ liệu đầu vào
                var validationResult = await ValidateTourData(tourDto);
                if (validationResult != null)
                {
                    return validationResult;
                }
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Tạo đối tượng tour từ DTO
                    var tour = MapToTourDetail(tourDto);
                    tour.CreateAt = DateTime.UtcNow;

                    _context.TourDetails.Add(tour);
                    await _context.SaveChangesAsync();
                    // Tạo CodeTour sau khi đã có ID
                    tour.CodeTour = GenerateCodeTour(tour.Id);
                    await _context.SaveChangesAsync();

                    // Xử lý các mối quan hệ
                    await AddTourRelationships(tour.Id, tourDto);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Tour đã được tạo: {NameTour} (ID: {Id})", tour.NameTour, tour.Id);

                    return CreatedAtAction(nameof(GetTourById), new { id = tour.Id }, MapToTourDetailDTO(tour));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi tạo tour");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo tour" });
            }
        }

        // 4. Cập nhật tour
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] CreateTourDetailDTO tourDto)
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

                var tour = await _context.TourDetails
                    .Include(t => t.TourCategoryMappings)
                    .Include(t => t.TourDestinations)
                    .Include(t => t.TourDepartures)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy khi cập nhật", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                // Kiểm tra tính hợp lệ của dữ liệu đầu vào (trừ URL nếu không thay đổi)
                var validationResult = await ValidateTourData(tourDto, id);
                if (validationResult != null)
                {
                    return validationResult;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cập nhật thông tin tour
                    UpdateTourProperties(tour, tourDto);

                    // Xóa các mối quan hệ
                    await RemoveRelationships(tour);
                    // Thêm các mối quan hệ mới
                    await AddTourRelationships(tour.Id, tourDto);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Tour đã được cập nhật: {NameTour} (ID: {Id})", tour.NameTour, tour.Id);

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
                _logger.LogError(ex, "Lỗi đồng thời xảy ra khi cập nhật tour với ID {Id}", id);
                return StatusCode(409, new { message = "Dữ liệu đã bị thay đổi bởi người khác" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi cập nhật tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật tour" });
            }
        }

        // 5. Xóa tour
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var tour = await _context.TourDetails
                    .Include(t => t.TourCategoryMappings)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy khi xóa", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Xóa các mối quan hệ
                    await RemoveRelationships(tour);
                    // Xóa các ngày khởi hành
                    _context.TourDates.RemoveRange(tour.TourDates);

                    // Xóa tour
                    _context.TourDetails.Remove(tour);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Tour đã được xóa: {NameTour} (ID: {Id})", tour.NameTour, tour.Id);

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
                _logger.LogError(ex, "Lỗi xảy ra khi xóa tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa tour" });
            }
        }

        // 6. Soft Delete - Đánh dấu tour đã xóa
        [HttpPut("{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteTour(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var tour = await _context.TourDetails.FindAsync(id);
                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy khi soft delete", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                tour.IsDelete = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tour đã được đánh dấu xóa: {NameTour} (ID: {Id})", tour.NameTour, tour.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi đánh dấu xóa tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đánh dấu xóa tour" });
            }
        }

        // 7. Chuyển đổi trạng thái Hot
        [HttpPut("{id}/toggle-hot")]
        public async Task<IActionResult> ToggleHotStatus(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var tour = await _context.TourDetails.FindAsync(id);
                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy khi chuyển trạng thái hot", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                tour.IsHot = !tour.IsHot;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tour ID {Id} đã chuyển trạng thái hot thành {IsHot}", id, tour.IsHot);

                return Ok(new { isHot = tour.IsHot });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi chuyển trạng thái hot cho tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi chuyển trạng thái hot" });
            }
        }

        // 8. Chuyển đổi trạng thái ẩn/hiện
        [HttpPut("{id}/toggle-hide")]
        public async Task<IActionResult> ToggleHideStatus(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "ID không hợp lệ" });
                }

                var tour = await _context.TourDetails.FindAsync(id);
                if (tour == null)
                {
                    _logger.LogWarning("Tour với ID {Id} không tìm thấy khi chuyển trạng thái ẩn/hiện", id);
                    return NotFound(new { message = "Tour không tồn tại." });
                }

                tour.IsHide = !tour.IsHide;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tour ID {Id} đã chuyển trạng thái ẩn/hiện thành {IsHide}", id, tour.IsHide);

                return Ok(new { isHide = tour.IsHide });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi chuyển trạng thái ẩn/hiện cho tour với ID {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi chuyển trạng thái ẩn/hiện" });
            }
        }

        //9. Lấy tour lọc theo danh mục...
        [HttpGet("filter/{categoryUrl}")]
        public async Task<ActionResult<IEnumerable<TourDetailDTO>>> FilterTours(
            string categoryUrl,
            [FromQuery] string? departure = null,
            [FromQuery] string? destination = null,
            [FromQuery] string? duration = null,
            [FromQuery] string? month = null,
            [FromQuery] string? transport = null,
            [FromQuery] string? sort = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                // Tìm danh mục theo URL
                var category = await _context.CategoryTours
                    .FirstOrDefaultAsync(c => c.Url == categoryUrl && !c.IsDeleted);
                if (category == null)
                {
                    _logger.LogWarning("Category with CategoryUrl {CategoryUrl} not found", categoryUrl);
                    return NotFound(new { message = $"Không tìm thấy danh mục tour với CategoryUrl: {categoryUrl}" });
                }
                // Lấy tất cả ID của danh mục hiện tại và các danh mục con
                var categoryIds = await _context.CategoryTours
                    .Where(c => c.Id == category.Id || c.ParentCategoryTourId == category.Id)
                    .Select(c => c.Id)
                    .ToListAsync();
                // Xây dựng truy vấn cơ bản
                var query = _context.TourDetails
                        .Include(t => t.MainCategoryTour)
                        .Include(t => t.TourCategoryMappings)
                            .ThenInclude(tcm => tcm.CategoryTour)
                        .Include(t => t.TourDepartures)
                            .ThenInclude(td => td.DeparturePoint)
                        .Include(t => t.TourDestinations)
                            .ThenInclude(td => td.Destination)
                        .Include(t => t.TourDates)
                        .Where(t => !t.IsDelete && !t.IsHide)
                        .AsQueryable();

                // Lọc tour theo danh mục chính hoặc danh mục liên quan
                query = query.Where(t =>
                    (t.MainCategoryTourId != null && categoryIds.Contains(t.MainCategoryTourId.Value)) ||
                    t.TourCategoryMappings.Any(tcm => categoryIds.Contains(tcm.CategoryTourId)));

                // Lọc theo điểm khởi hành
                if (!string.IsNullOrEmpty(departure))
                {
                    var departurePoints = departure.Split(',').Select(d => d.Trim().ToLower()).ToList();
                    query = query.Where(t => t.TourDepartures.Any(td =>
                        departurePoints.Contains(td.DeparturePoint.Name.ToLower())));
                }

                // // Lọc theo điểm đến
                if (!string.IsNullOrEmpty(destination))
                {
                    var destinations = destination.Split(',').Select(d => d.Trim().ToLower()).ToList();
                    query = query.Where(t => t.TourDestinations.Any(td =>
                        destinations.Contains(td.Destination.Name.ToLower())));
                }

                // Lọc theo thời gian (duration)
                if (!string.IsNullOrEmpty(duration))
                {
                    // Chỉ xử lý với tours có Timeline
                    var toursWithTimeline = await query.Where(t => t.Timeline != null).ToListAsync();

                    // Danh sách ID của các tour thỏa mãn điều kiện duration
                    List<int> filteredTourIds;

                    switch (duration.ToLower())
                    {
                        case "under10":
                            filteredTourIds = toursWithTimeline
                                .Where(t => t.Timeline.ToLower().Contains("ngày") &&
                                    int.TryParse(t.Timeline.Split(' ')[0], out int days1) &&
                                    days1 < 10)
                                .Select(t => t.Id)
                                .ToList();
                            break;
                        case "over10":
                            filteredTourIds = toursWithTimeline
                                .Where(t => t.Timeline.ToLower().Contains("ngày") &&
                                    int.TryParse(t.Timeline.Split(' ')[0], out int days2) &&
                                    days2 >= 10)
                                .Select(t => t.Id)
                                .ToList();
                            break;
                        case "under7":
                            filteredTourIds = toursWithTimeline
                                .Where(t => t.Timeline.ToLower().Contains("ngày") &&
                                    int.TryParse(t.Timeline.Split(' ')[0], out int days3) &&
                                    days3 < 7)
                                .Select(t => t.Id)
                                .ToList();
                            break;
                        default:
                            filteredTourIds = toursWithTimeline.Select(t => t.Id).ToList();
                            break;
                    }
                    // Áp dụng lọc theo ID vào query gốc
                    query = query.Where(t => filteredTourIds.Contains(t.Id));
                }

                // Lọc theo tháng khởi hành
                if (!string.IsNullOrEmpty(month))
                {
                    // Định dạng month là "MM-YYYY"
                    if (DateTime.TryParseExact(month, "MM-yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime targetDate))
                    {
                        var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                        query = query.Where(t => t.TourDates.Any(td =>
                            td.StartDate >= startOfMonth && td.StartDate <= endOfMonth));
                    }
                }

                // Lọc theo phương tiện vận chuyển
                if (!string.IsNullOrEmpty(transport))
                {
                    if (transport.Equals("plane", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Flight != null && t.Flight.ToLower().Contains("hàng không"));
                    }
                    else if (transport.Equals("car", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(t => t.Flight != null && t.Flight.ToLower().Contains("ô tô"));
                    }
                }

                // Lọc theo khoảng giá
                if (minPrice.HasValue)
                {
                    query = query.Where(t =>
                        (t.PromotionallPrice.HasValue && t.PromotionallPrice >= minPrice) ||
                        (!t.PromotionallPrice.HasValue && t.OriginalPrice.HasValue && t.OriginalPrice >= minPrice));
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(t =>
                        (t.PromotionallPrice.HasValue && t.PromotionallPrice <= maxPrice) ||
                        (!t.PromotionallPrice.HasValue && t.OriginalPrice.HasValue && t.OriginalPrice <= maxPrice));
                }

                // Sắp xếp kết quả
                if (!string.IsNullOrEmpty(sort))
                {
                    switch (sort.ToLower())
                    {
                        case "price-asc":
                            query = query.OrderBy(t => t.PromotionallPrice);
                            break;
                        case "price-desc":
                            query = query.OrderByDescending(t => t.PromotionallPrice);
                            break;
                        case "date-asc":
                            query = query.OrderBy(t => t.TourDates.Min(td => td.StartDate));
                            break;
                        case "date-desc":
                            query = query.OrderByDescending(t => t.TourDates.Min(td => td.StartDate));
                            break;
                        default:
                            query = query.OrderByDescending(t => t.CreateAt);
                            break;
                    }
                }
                else
                {
                    // Mặc định sắp xếp theo ngày tạo mới nhất
                    query = query.OrderByDescending(t => t.CreateAt);
                }

                // Đếm tổng số tour thỏa mãn điều kiện lọc
                var totalTours = await query.CountAsync();

                // Phân trang
                var tours = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TourDetailDTO
                    {
                        Id = t.Id,
                        NameTour = t.NameTour,
                        OriginalPrice = t.OriginalPrice,
                        PromotionallPrice = t.PromotionallPrice,
                        Timeline = t.Timeline,
                        Url = t.Url,
                        Flight = t.Flight,
                        Avatar = t.Avatar,
                        IsHot = t.IsHot,
                        MainCategoryTourId = t.MainCategoryTourId,
                        MainCategoryTourName = t.MainCategoryTour != null ? t.MainCategoryTour.CategoryName : null,
                        RelatedCategories = t.TourCategoryMappings
                            .Select(tcm => new ChildTourDTO
                            {
                                Id = tcm.CategoryTourId,
                                Name = tcm.CategoryTour.CategoryName
                            }).ToList(),
                        RelatedDestinations = t.TourDestinations
                            .Select(td => new ChildTourDTO
                            {
                                Id = td.DestinationId,
                                Name = td.Destination.Name
                            }).ToList(),
                        RelatedDeparturePoints = t.TourDepartures
                            .Select(td => new ChildTourDTO
                            {
                                Id = td.DeparturePointId,
                                Name = td.DeparturePoint.Name
                            }).ToList(),
                        RelatedTourDates = t.TourDates
                            .Select(td => new TourDateDTO
                            {
                                Id = td.Id,
                                StartDate = td.StartDate
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    items = tours,
                    totalCount = totalTours,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalTours / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi lọc danh sách tour");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lọc danh sách tour", error = ex.Message });
            }
        }


        // Hàm chuyển đổi TourDetail sang TourDetailDTO
        private TourDetailDTO MapToTourDetailDTO(TourDetail tour)
        {
            if (tour == null)
                return null;

            return new TourDetailDTO
            {
                Id = tour.Id,
                CodeTour = tour.CodeTour,
                NameTour = tour.NameTour,
                OriginalPrice = tour.OriginalPrice,
                PromotionallPrice = tour.PromotionallPrice,
                CountryFrom = tour.CountryFrom,
                CountryTo = tour.CountryTo,
                Hotel = tour.Hotel,
                Flight = tour.Flight,
                Notes = tour.Notes,
                Timeline = tour.Timeline,
                Description = tour.Description,
                Url = tour.Url,
                Promotion = tour.Promotion,
                Avatar = tour.Avatar,
                CreateAt = tour.CreateAt,
                Creater = tour.Creater,
                IsHot = tour.IsHot,
                IsHide = tour.IsHide,
                MainCategoryTourId = tour.MainCategoryTourId,
                MainCategoryTourName = tour.MainCategoryTour?.CategoryName,
                RelatedCategories = tour.TourCategoryMappings?
                    .Where(tcm => tcm.CategoryTour != null)
                    .Select(tcm => new ChildTourDTO
                    {
                        Id = tcm.CategoryTourId,
                        Name = tcm.CategoryTour.CategoryName
                    }).ToList() ?? new List<ChildTourDTO>()
            };
        }

        // Kiểm tra tính hợp lệ của dữ liệu tour
        private async Task<ActionResult> ValidateTourData(CreateTourDetailDTO tourDto, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(tourDto.NameTour))
            {
                return BadRequest(new { message = "Tên tour không được để trống" });
            }

            if (string.IsNullOrWhiteSpace(tourDto.Url))
            {
                return BadRequest(new { message = "Url không được để trống" });
            }

            // Kiểm tra URL đã tồn tại (nếu là cập nhật thì loại trừ ID hiện tại)
            var urlExists = excludeId.HasValue
                ? await _context.TourDetails.AnyAsync(t => t.Url == tourDto.Url && t.Id != excludeId)
                : await _context.TourDetails.AnyAsync(t => t.Url == tourDto.Url);

            if (urlExists)
            {
                return BadRequest(new { message = "Url đã tồn tại." });
            }
            // Kiểm tra CodeTour đã tồn tại (nếu là cập nhật thì loại trừ ID hiện tại)
            var codeTourExists = excludeId.HasValue
                ? await _context.TourDetails.AnyAsync(t => t.CodeTour == tourDto.CodeTour && t.Id != excludeId)
                : await _context.TourDetails.AnyAsync(t => t.CodeTour == tourDto.CodeTour);

            if (codeTourExists)
            {
                return BadRequest(new { message = "Url đã tồn tại." });
            }

            // Kiểm tra danh mục chính
            var mainCategoryExists = await _context.CategoryTours.AnyAsync(c => c.Id == tourDto.MainCategoryTourId);
            if (!mainCategoryExists)
            {
                return BadRequest(new { message = "Danh mục chính không tồn tại." });
            }

            return null;
        }

        // Chuyển đổi từ DTO sang entity
        private TourDetail MapToTourDetail(CreateTourDetailDTO tourDto)
        {
            return new TourDetail
            {
                NameTour = tourDto.NameTour.Trim(),
                OriginalPrice = tourDto.OriginalPrice,
                PromotionallPrice = tourDto.PromotionallPrice,
                CountryFrom = tourDto.CountryFrom,
                CountryTo = tourDto.CountryTo,
                Hotel = tourDto.Hotel?.Trim(),
                Flight = tourDto.Flight?.Trim(),
                Notes = tourDto.Notes?.Trim(),
                Timeline = tourDto.Timeline?.Trim(),
                Description = tourDto.Description,
                Url = tourDto.Url.Trim(),
                Promotion = tourDto.Promotion,
                Avatar = tourDto.Avatar,
                Creater = tourDto.Creater,
                IsHot = tourDto.IsHot,
                IsHide = tourDto.IsHide,
                MainCategoryTourId = tourDto.MainCategoryTourId
            };
        }

        // Cập nhật thuộc tính tour
        private void UpdateTourProperties(TourDetail tour, CreateTourDetailDTO tourDto)
        {
            tour.CodeTour = tourDto.CodeTour.Trim();
            tour.NameTour = tourDto.NameTour.Trim();
            tour.OriginalPrice = tourDto.OriginalPrice;
            tour.PromotionallPrice = tourDto.PromotionallPrice;
            tour.CountryFrom = tourDto.CountryFrom;
            tour.CountryTo = tourDto.CountryTo;
            tour.Hotel = tourDto.Hotel?.Trim();
            tour.Flight = tourDto.Flight?.Trim();
            tour.Notes = tourDto.Notes?.Trim();
            tour.Timeline = tourDto.Timeline?.Trim();
            tour.Description = tourDto.Description;
            tour.Url = tourDto.Url.Trim();
            tour.Promotion = tourDto.Promotion;
            tour.Avatar = tourDto.Avatar;
            tour.IsHot = tourDto.IsHot;
            tour.IsHide = tourDto.IsHide;
            tour.MainCategoryTourId = tourDto.MainCategoryTourId;
        }

        // Thêm các mối quan hệ cho tour
        private async Task AddTourRelationships(int tourId, CreateTourDetailDTO tourDto)
        {
            // Thêm các danh mục tour liên quan
            if (tourDto.RelatedCategoryIds?.Any() == true)
            {
                var validCategoryIds = await _context.CategoryTours
                    .Where(c => tourDto.RelatedCategoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                var categoryMappings = validCategoryIds.Select(categoryId => new TourCategoryMapping
                {
                    TourDetailId = tourId,
                    CategoryTourId = categoryId
                });

                _context.TourCategoryMappings.AddRange(categoryMappings);
                await _context.SaveChangesAsync();
            }

            // Thêm các địa điểm đến trong tour
            if (tourDto.RelatedDestinationIds?.Any() == true)
            {
                var validDestinationIds = await _context.Destinations
                    .Where(d => tourDto.RelatedDestinationIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync();

                var destinationMappings = validDestinationIds.Select(destinationId => new TourDestination
                {
                    TourDetailId = tourId,
                    DestinationId = destinationId
                });

                _context.TourDestinations.AddRange(destinationMappings);
                await _context.SaveChangesAsync();
            }

            // Thêm các địa điểm xuất phát trong tour
            if (tourDto.RelatedDeparturePointIds?.Any() == true)
            {
                var validDepartureIds = await _context.DeparturePoints
                    .Where(d => tourDto.RelatedDeparturePointIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync();

                var departureMappings = validDepartureIds.Select(departureId => new TourDeparture
                {
                    TourDetailId = tourId,
                    DeparturePointId = departureId
                });

                _context.TourDepartures.AddRange(departureMappings);
                await _context.SaveChangesAsync();
            }
        }

        // Xóa các mối quan hệ cho tour
        private async Task RemoveRelationships(TourDetail tour)
        {
            // Xóa các mối quan hệ cũ
            _context.TourCategoryMappings.RemoveRange(tour.TourCategoryMappings);
            _context.TourDestinations.RemoveRange(tour.TourDestinations);
            _context.TourDepartures.RemoveRange(tour.TourDepartures);
            await _context.SaveChangesAsync();
        }

        //Sinh mã CodeTour 
        private string GenerateCodeTour(int tourId)
        {
            // Tạo 6 ký tự ngẫu nhiên in hoa
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var randomPart = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Kết hợp với ID tour để đảm bảo tính duy nhất
            return $"TOUR-{randomPart}-{tourId}";
        }




    }
}

