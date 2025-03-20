using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class StoredProcedureController : ControllerBase
{
    private readonly StoredProcedureService _spService;

    public StoredProcedureController(StoredProcedureService spService)
    {
        _spService = spService;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteProcedure([FromBody] ProcedureRequest request)
    {
        // Kiểm tra dữ liệu đầu vào
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Nếu không có tham số, truyền danh sách rỗng để tránh lỗi null
            var parameters = request.Parameters ?? new List<ProcedureParameter>();
            var result = await _spService.ExecuteProcedureAsync(request.ProcedureName, parameters);

            // Kiểm tra dữ liệu trả về
            if (result == null || result.Count == 0)
            {
                return NotFound("Không tìm thấy dữ liệu.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi thực thi stored procedure: {ex.Message}");
        }
    }
}

// DTO cho request
public class ProcedureRequest
{
    [Required(ErrorMessage = "ProcedureName không được để trống")]
    public string ProcedureName { get; set; }

    public List<ProcedureParameter>? Parameters { get; set; }
}


