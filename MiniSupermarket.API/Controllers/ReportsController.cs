using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using MiniSupermarket.API.Services;

namespace MiniSupermarket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public partial class ReportsController : ControllerBase
    {
        private readonly SupermarketDbContext _context;
        private readonly RevenueReportService _reports;
        public ReportsController(SupermarketDbContext context, RevenueReportService reports) { _context = context; _reports = reports; }

        [HttpGet("revenue-by-cashier")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevenueByCashier(CancellationToken cancellationToken)
        {
            var rows = await _reports.ByCashierAsync(null, null, cancellationToken);
            return Ok(new RevenueReportDto
            {
                GeneratedAt = DateTime.Now, TotalCashiers = rows.Count, GrandTotalRevenue = rows.Sum(r => r.TotalRevenue), Data = rows
            });
        }

        [HttpGet("revenue-by-date-range")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevenueByDateRange(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            if (fromDate == default || toDate == default || fromDate.Date > toDate.Date || toDate.Date == DateTime.MaxValue.Date)
                return BadRequest(new { message = "Khoảng ngày báo cáo không hợp lệ." });
            var rows = await _reports.ByCashierAsync(fromDate.Date, toDate.Date.AddDays(1), cancellationToken);
            return Ok(new RevenueReportDto
            {
                GeneratedAt = DateTime.Now, TotalCashiers = rows.Count, GrandTotalRevenue = rows.Sum(r => r.TotalRevenue), Data = rows
            });
        }

        [HttpGet("top-selling")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> TopSelling(CancellationToken cancellationToken)
        {
            List<TopProductDto> rows;
            // LINQ GroupBy -> Sum -> OrderByDescending -> Take(5); số lượng là tiêu chí xếp hạng.
            if (_context.Database.IsSqlServer())
                rows = await _context.OrderDetails.AsNoTracking().Where(d => d.Order!.Status == "Hoàn tất")
                    .GroupBy(d => new { d.ProductId, d.Product!.ProductName })
                    .Select(g => new TopProductDto { ProductId = g.Key.ProductId, ProductName = g.Key.ProductName,
                        TotalQuantity = g.Sum(d => (long)d.Quantity), TotalRevenue = g.Sum(d => d.LineTotal) })
                    .OrderByDescending(p => p.TotalQuantity).ThenBy(p => p.ProductId).Take(5).ToListAsync(cancellationToken);
            else rows = await _reports.TopProductsAsync(cancellationToken);
            // Thủ kho chỉ nhận mã/tên/số lượng để nhập hàng; doanh số dành cho Admin.
            if (!User.IsInRole("Admin")) return Ok(rows.Select(p => new { p.ProductId, p.ProductName, p.TotalQuantity }));
            return Ok(rows);
        }

        [HttpGet("quick")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Quick([FromQuery] DateTime date)
        {
            if (date == default || date.Date == DateTime.MaxValue.Date)
                return BadRequest(new { message = "Vui lòng chọn ngày báo cáo hợp lệ." });
            var start = date.Date;
            var end = start.AddDays(1);
            var orders = _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= start && o.OrderDate < end && o.Status == "Hoàn tất");
            var totals = await orders.Select(o => o.FinalAmount).ToListAsync();
            var lines = await _context.OrderDetails.AsNoTracking()
                .Where(d => d.Order!.OrderDate >= start && d.Order.OrderDate < end && d.Order.Status == "Hoàn tất")
                .Select(d => new { d.ProductId, d.Product!.ProductName, d.Quantity }).ToListAsync();
            var best = lines.GroupBy(d => new { d.ProductId, d.ProductName })
                .Select(g => new { g.Key.ProductId, g.Key.ProductName, Quantity = g.Sum(d => (long)d.Quantity) })
                .OrderByDescending(g => g.Quantity).ThenBy(g => g.ProductId).FirstOrDefault();
            return Ok(new QuickReportDto
            {
                Date = start, TotalOrders = totals.Count, TotalRevenue = totals.Sum(),
                BestSeller = best?.ProductName ?? "Chưa có giao dịch", BestSellerQuantity = best?.Quantity ?? 0
            });
        }
    }
}
