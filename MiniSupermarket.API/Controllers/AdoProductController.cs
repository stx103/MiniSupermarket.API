using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using static MiniSupermarket.API.Data.AdoDatabase;

namespace MiniSupermarket.API.Controllers
{
    [ApiController]
    [Route("api/ado-products")]
    [Authorize(Roles = "Admin,Cashier,Warehouse")]
    public class AdoProductController : ControllerBase
    {
        private const string ProductQuery = "SELECT p.ProductId, p.Barcode, p.ProductName, p.Price, p.StockQuantity, p.CategoryId, c.CategoryName FROM Products p INNER JOIN Categories c ON c.CategoryId = p.CategoryId";
        private readonly AdoDatabase _database;
        public AdoProductController(AdoDatabase database) => _database = database;

        // [Tiêu chí 32] Tra barcode bằng ADO.NET: AdoDatabase lấy SqlConnection và tạo SqlCommand khi dùng SQL Server.
        [HttpGet("barcode/{barcode}")]
        [ProducesResponseType(typeof(ProductItemDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(barcode) || barcode.Trim().Length > 50) return BadRequest(new { message = "Mã vạch không hợp lệ." });
            await using var lease = await _database.OpenAsync(cancellationToken);
            using var command = Command(lease.Connection, ProductQuery + " WHERE p.Barcode = @Barcode");
            // [Tiêu chí 33] Giá trị nhập đi qua tham số @Barcode (helper dùng AddWithValue), không nối giá trị vào chuỗi SQL.
            Parameter(command, "@Barcode", barcode.Trim(), DbType.String, 50);
            // [Tiêu chí 33] SQL Server trả SqlDataReader; ReadAsync đọc tuần tự, using đóng reader/command và await using trả kết nối.
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Ok(ReadProduct(reader))
                : NotFound(new { message = "Không tìm thấy mã vạch sản phẩm." });
        }

        [HttpGet("filter-stock")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> FilterStock([Range(0, int.MaxValue)] int minStock = 50, CancellationToken cancellationToken = default)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            using var command = Command(lease.Connection, ProductQuery + " WHERE p.StockQuantity <= @Stock ORDER BY p.StockQuantity, p.ProductId");
            Parameter(command, "@Stock", minStock, DbType.Int32);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<ProductItemDto>();
            while (await reader.ReadAsync(cancellationToken)) rows.Add(ReadProduct(reader));
            return Ok(rows);
        }

        [HttpGet("count")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Count(CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            using var command = Command(lease.Connection, "SELECT COUNT(*) FROM Products");
            return Ok(new ProductCountDto { Total = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) });
        }

        [HttpPut("{id:int}/stock")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> UpdateStock(int id, StockUpdateRequest request, CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            using var command = Command(lease.Connection, "UPDATE Products SET StockQuantity = @Stock WHERE ProductId = @Id");
            Parameter(command, "@Stock", request.StockQuantity, DbType.Int32);
            Parameter(command, "@Id", id, DbType.Int32);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1 ? NoContent() : NotFound();
        }

        [HttpGet("disconnected-cache")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> DisconnectedCache(CancellationToken cancellationToken)
        {
            using var command = Command(_database.Connection, ProductQuery + " ORDER BY p.ProductId");
            using var table = await FillTableAsync(command, cancellationToken);
            var rows = table.Rows.Cast<DataRow>().Select(row => new ProductItemDto
            {
                ProductId = Convert.ToInt32(row["ProductId"]), Barcode = Convert.ToString(row["Barcode"])!,
                ProductName = Convert.ToString(row["ProductName"])!, Price = Convert.ToDecimal(row["Price"]),
                StockQuantity = Convert.ToInt32(row["StockQuantity"]), CategoryId = Convert.ToInt32(row["CategoryId"]),
                CategoryName = Convert.ToString(row["CategoryName"])!
            }).ToList();
            return Ok(new ProductCacheDto { TotalLoaded = rows.Count, CachedAt = DateTime.Now, Data = rows });
        }

        [HttpGet("category-counts")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> CategoryCounts(CancellationToken cancellationToken)
        {
            using var command = Command(_database.Connection,
                "SELECT c.CategoryId, c.CategoryName, COUNT(p.ProductId) AS Total FROM Categories c LEFT JOIN Products p ON p.CategoryId = c.CategoryId GROUP BY c.CategoryId, c.CategoryName ORDER BY c.CategoryId");
            using var table = await FillTableAsync(command, cancellationToken);
            return Ok(table.Rows.Cast<DataRow>().Select(row => new CategoryCountDto
            {
                CategoryId = Convert.ToInt32(row["CategoryId"]), CategoryName = Convert.ToString(row["CategoryName"])!,
                Total = Convert.ToInt32(row["Total"])
            }).ToList());
        }

        private async Task<DataTable> FillTableAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var table = new DataTable();
            try
            {
                if (command is SqlCommand sqlCommand)
                {
                    // Fill opens/closes a closed SqlConnection; the resulting table is disconnected.
                    using var adapter = new SqlDataAdapter(sqlCommand);
                    adapter.Fill(table);
                }
                else
                {
                    await using var lease = await _database.OpenAsync(cancellationToken);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    table.Load(reader);
                }
                return table;
            }
            catch { table.Dispose(); throw; }
        }

        private static ProductItemDto ReadProduct(DbDataReader reader) => new()
        {
            ProductId = reader.GetInt32(0), Barcode = reader.GetString(1), ProductName = reader.GetString(2),
            Price = Convert.ToDecimal(reader.GetValue(3)), StockQuantity = reader.GetInt32(4),
            CategoryId = reader.GetInt32(5), CategoryName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
        };
    }
}
