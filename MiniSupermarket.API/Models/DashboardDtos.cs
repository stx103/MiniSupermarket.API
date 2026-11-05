namespace MiniSupermarket.API.Models;

public sealed class DashboardSummaryDto
{
    public DateTime Date { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int TotalOrders { get; set; }
    public DateTime? LatestOrderDate { get; set; }
    public int NewCustomers { get; set; }
    public int LowStockProducts { get; set; }
    public int LowStockThreshold => 10;
}

public sealed class DailyRevenueDto
{
    public DateTime PeriodStart { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class InvoiceReportDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string CashierUsername { get; set; } = "";
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public int RedeemedPoints { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
