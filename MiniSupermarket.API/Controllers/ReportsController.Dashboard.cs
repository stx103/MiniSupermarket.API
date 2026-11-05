using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSupermarket.API.Models;

namespace MiniSupermarket.API.Controllers;

public partial class ReportsController
{
    // KPI luôn là hôm nay theo đồng hồ máy chủ; khách cũ CreatedAt=NULL không tính là mới.
    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DashboardSummary(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var until = today.AddDays(1);
        var sales = _context.Orders.AsNoTracking().Where(o => o.Status == "Hoàn tất" && o.OrderDate >= today && o.OrderDate < until);
        decimal total = _context.Database.IsSqlServer()
            ? await sales.SumAsync(o => (decimal?)o.FinalAmount, cancellationToken) ?? 0
            : (await sales.Select(o => o.FinalAmount).ToListAsync(cancellationToken)).Sum();
        return Ok(new DashboardSummaryDto
        {
            Date = today, TodayRevenue = total,
            TodayOrders = await sales.CountAsync(cancellationToken),
            TotalOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.Status == "Hoàn tất", cancellationToken),
            LatestOrderDate = await _context.Orders.AsNoTracking().Where(o => o.Status == "Hoàn tất")
                .MaxAsync(o => (DateTime?)o.OrderDate, cancellationToken),
            NewCustomers = await _context.Customers.AsNoTracking().CountAsync(c => c.CreatedAt >= today && c.CreatedAt < until, cancellationToken),
            LowStockProducts = await _context.Products.AsNoTracking().CountAsync(p => p.StockQuantity <= 10, cancellationToken)
        });
    }

    [HttpGet("daily-revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DailyRevenue(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken, string period = "day")
    {
        if (!ValidRange(fromDate, toDate) || period is not ("day" or "week" or "month"))
            return BadRequest(new { message = "Chọn khoảng ngày hợp lệ và kỳ day, week hoặc month." });
        return Ok(await _reports.DailyAsync(fromDate.Date, toDate.Date.AddDays(1), period, cancellationToken));
    }

    [HttpGet("orders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Invoices(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        if (!ValidRange(fromDate, toDate)) return BadRequest(new { message = "Khoảng ngày báo cáo không hợp lệ." });
        var from = fromDate.Date; var until = toDate.Date.AddDays(1);
        return Ok(await _context.Orders.AsNoTracking()
            .Where(o => o.Status == "Hoàn tất" && o.OrderDate >= from && o.OrderDate < until)
            .OrderBy(o => o.OrderDate).ThenBy(o => o.OrderId)
            .Select(o => new InvoiceReportDto
            {
                OrderId = o.OrderId, OrderCode = o.OrderCode, OrderDate = o.OrderDate,
                CashierUsername = o.User!.Username, CustomerName = o.Customer == null ? null : o.Customer.CustomerName,
                TotalAmount = o.TotalAmount, RedeemedPoints = o.RedeemedPoints,
                DiscountAmount = o.DiscountAmount, FinalAmount = o.FinalAmount
            }).ToListAsync(cancellationToken));
    }

    private static bool ValidRange(DateTime from, DateTime to) => from != default && to != default
        && from.Date <= to.Date && (to.Date - from.Date).TotalDays <= 3660 && to.Date < DateTime.MaxValue.Date;
}
