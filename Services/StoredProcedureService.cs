using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using TravelAPI.Data;

public class StoredProcedureService
{
    private readonly TravelDbContext _context;

    public StoredProcedureService(TravelDbContext context)
    {
        _context = context;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteProcedureAsync(string procedureName, List<ProcedureParameter> parameters)
    {
        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            // Thêm các tham số vào command
            // foreach (var param in parameters)
            // {
            //     var sqlParam = new SqlParameter(param.Name, param.Value ?? DBNull.Value);
            //     command.Parameters.Add(sqlParam);
            // }
            foreach (var param in parameters)
            {
                object value = param.Value;

                // Nếu giá trị là JsonElement, chuyển đổi sang kiểu dữ liệu thích hợp
                if (value is JsonElement jsonElement)
                {
                    // Chuyển đổi JsonElement sang kiểu dữ liệu gốc
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        value = jsonElement.GetString();
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.Number)
                    {
                        value = jsonElement.TryGetInt32(out int intValue) ? intValue : (object)jsonElement.GetDecimal();
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False)
                    {
                        value = jsonElement.GetBoolean();
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.Null)
                    {
                        value = DBNull.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported JsonElement type: {jsonElement.ValueKind}");
                    }
                }

                var sqlParam = new SqlParameter(param.Name, value ?? DBNull.Value);
                command.Parameters.Add(sqlParam);
            }


            await _context.Database.OpenConnectionAsync();

            using (var reader = await command.ExecuteReaderAsync())
            {
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                return results;
            }
        }
    }
}
/* cách 2 - cần tạo 
public async Task<List<dynamic>> ExecuteProcedureAsync(string procedureName, List<ProcedureParameter> parameters)
    {
        // Tạo danh sách tham số SQL
        var sqlParams = parameters.Select(p => new SqlParameter(p.Name, p.Value ?? DBNull.Value)).ToArray();

        // Xây dựng câu lệnh SQL động
        string sqlQuery = $"EXEC {procedureName} {string.Join(", ", sqlParams.Select(p => p.ParameterName))}";

        // Thực thi stored procedure và lấy kết quả
        var result = await _context.SomeEntity.FromSqlRaw(sqlQuery, sqlParams).ToListAsync();

        return result.Cast<dynamic>().ToList();
    }

*/

// DTO cho tham số stored procedure
public class ProcedureParameter
{
    [Required(ErrorMessage = "Tên tham số không được để trống")]
    public string Name { get; set; }

    public object? Value { get; set; }
}
