using System.Windows.Forms.DataVisualization.Charting;

namespace MiniSupermarket.WinForms;

public partial class FormQuickReport : Form
{
    private List<DailyRevenueView> _revenue = new();
    private List<InvoiceView> _invoices = new();
    private bool _busy;
    public FormQuickReport() { InitializeComponent(); WorkspaceUi.ApplyRolePermissions(this, "Reports"); }

    private async Task LoadReportsAsync(bool selectLatestMonth = false)
    {
        if (_busy || !WorkspaceUi.ApplyRolePermissions(this, "Reports")) return;
        if (dtpFromDate.Value.Date > dtpToDate.Value.Date) { WorkspaceUi.Invalid("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc."); return; }
        _busy = true; btnRunRange.Enabled = btnLatestMonth.Enabled = btnExportRevenue.Enabled = btnExportInvoices.Enabled = false;
        dtpFromDate.Enabled = dtpToDate.Enabled = cboPeriod.Enabled = false;
        try
        {
            // KPI luôn là hôm nay; khoảng ngày áp dụng cho biểu đồ, thu ngân và hóa đơn.
            var summary = await ApiClientService.GetAsync<DashboardSummaryView>("reports/dashboard-summary");
            if (IsDisposed) return;
            // Hóa đơn mẫu có ngày cũ: mở đúng tháng có giao dịch, giữ nguyên ngày/tiền đã lưu.
            // Các lần bấm Xem báo cáo vẫn tôn trọng khoảng ngày người dùng tự chọn.
            if (selectLatestMonth)
            {
                var latestOrderDate = summary.LatestOrderDate;
                // Tương thích khi WinForms được cập nhật trước API phiên bản cũ.
                if (latestOrderDate == null && summary.TotalOrders > 0)
                {
                    var until = summary.Date.Date;
                    var history = await ApiClientService.GetAsync<List<InvoiceView>>(
                        $"reports/orders?fromDate={until.AddYears(-10):yyyy-MM-dd}&toDate={until:yyyy-MM-dd}");
                    if (IsDisposed) return;
                    latestOrderDate = history.Count == 0 ? null : history.Max(x => x.OrderDate);
                }
                var latest = (latestOrderDate ?? summary.Date).Date;
                latest = latest < dtpFromDate.MinDate.Date ? dtpFromDate.MinDate.Date
                    : latest > dtpToDate.MaxDate.Date ? dtpToDate.MaxDate.Date : latest;
                var monthStart = new DateTime(latest.Year, latest.Month, 1);
                dtpFromDate.Value = monthStart;
                dtpToDate.Value = monthStart.AddMonths(1).AddDays(-1);
            }
            var range = $"fromDate={dtpFromDate.Value:yyyy-MM-dd}&toDate={dtpToDate.Value:yyyy-MM-dd}";
            string period = new[] { "day", "week", "month" }[Math.Max(0, cboPeriod.SelectedIndex)];
            var revenue = await ApiClientService.GetAsync<List<DailyRevenueView>>($"reports/daily-revenue?{range}&period={period}");
            var cashiers = await ApiClientService.GetAsync<RevenueEnvelope>($"reports/revenue-by-date-range?{range}");
            var top = await ApiClientService.GetAsync<List<TopProductView>>("reports/top-selling");
            var invoices = await ApiClientService.GetAsync<List<InvoiceView>>($"reports/orders?{range}");
            if (IsDisposed) return;
            lblTotalRevenue.Text = summary.TodayRevenue.ToString("N0") + " đ"; lblTotalOrders.Text = summary.TotalOrders.ToString("N0");
            lblNewCustomers.Text = summary.NewCustomers.ToString("N0"); lblLowStock.Text = summary.LowStockProducts.ToString("N0");
            _revenue = revenue; _invoices = invoices;
            dgvCashierRevenue.DataSource = cashiers.Data; dgvTopProducts.DataSource = top;
            dgvInvoices.DataSource = invoices; dgvDailyRevenue.DataSource = revenue;
            var series = revenueChart.Series[0]; series.Points.Clear(); revenueChart.Annotations.Clear();
            revenueChart.ChartAreas[0].AxisX.Interval = Math.Max(1, Math.Ceiling(revenue.Count / 15d));
            foreach (var row in revenue)
            {
                int index = series.Points.AddXY(row.PeriodStart.ToString(period == "month" ? "MM/yyyy" : "dd/MM"), row.TotalRevenue);
                series.Points[index].IsValueShownAsLabel = row.TotalRevenue > 0;
                series.Points[index].LabelFormat = "N0";
            }
            if (invoices.Count == 0 || invoices.All(x => x.FinalAmount == 0))
                revenueChart.Annotations.Add(new TextAnnotation
                {
                    Text = invoices.Count == 0
                        ? "Khoảng ngày này chưa có hóa đơn.\nChọn 'Tháng có hóa đơn' để xem doanh thu gần nhất."
                        : "Các hóa đơn trong khoảng chọn có thực thu bằng 0 đ.",
                    X = 5, Y = 35, Width = 90, Height = 25, Alignment = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11F), ForeColor = Color.DimGray
                });
            revenueChart.Titles[0].Text = $"Doanh thu thực thu ({cboPeriod.Text.ToLower()}) · {dtpFromDate.Value:dd/MM/yyyy} – {dtpToDate.Value:dd/MM/yyyy}";
            lblRangeTotal.Text = $"{invoices.Count:N0} hóa đơn · Thực thu {invoices.Sum(x => x.FinalAmount):N0} đ · Top 5 tính trên tất cả ngày";
            lblRangeTotal.ForeColor = invoices.Count == 0 ? Color.DarkOrange : Color.Black;
        }
        catch (Exception ex)
        {
            _revenue = new(); _invoices = new();
            if (!IsDisposed)
            {
                revenueChart.Series[0].Points.Clear(); revenueChart.Annotations.Clear();
                dgvInvoices.DataSource = null; dgvDailyRevenue.DataSource = null;
                lblRangeTotal.Text = "Không tải được báo cáo. Vui lòng thử lại.";
                WorkspaceUi.Error(ex);
            }
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
            {
                btnRunRange.Enabled = btnLatestMonth.Enabled = dtpFromDate.Enabled = dtpToDate.Enabled = cboPeriod.Enabled = true;
                btnExportRevenue.Enabled = _revenue.Count > 0; btnExportInvoices.Enabled = _invoices.Count > 0;
            }
        }
    }
    private void ExportRevenue(object? sender, EventArgs e)
    {
        try { CsvExport.Save(this, $"Doanh_thu_{dtpFromDate.Value:yyyyMMdd}_{dtpToDate.Value:yyyyMMdd}.csv",
            new[] { "Bắt đầu kỳ", "Số hóa đơn", "Doanh thu thực thu" },
            _revenue.Select(x => new object?[] { x.PeriodStart, x.TotalOrders, x.TotalRevenue })); }
        catch (Exception ex) { WorkspaceUi.Error(ex); }
    }
    private void ExportInvoices(object? sender, EventArgs e)
    {
        try { CsvExport.Save(this, $"Hoa_don_{dtpFromDate.Value:yyyyMMdd}_{dtpToDate.Value:yyyyMMdd}.csv",
            new[] { "ID", "Mã hóa đơn", "Ngày lập", "Thu ngân", "Khách hàng", "Tiền hàng", "Điểm đổi", "Giảm giá", "Thực thu" },
            _invoices.Select(x => new object?[] { x.OrderId, x.OrderCode, x.OrderDate, x.CashierUsername, x.CustomerName, x.TotalAmount, x.RedeemedPoints, x.DiscountAmount, x.FinalAmount })); }
        catch (Exception ex) { WorkspaceUi.Error(ex); }
    }
}
public class DashboardSummaryView
{
    public DateTime Date { get; set; }
    public DateTime? LatestOrderDate { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int TotalOrders { get; set; }
    public int NewCustomers { get; set; }
    public int LowStockProducts { get; set; }
}
public class DailyRevenueView
{
    public DateTime PeriodStart { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}
public class InvoiceView
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
public class QuickReportDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public string BestSeller { get; set; } = "";
    public long BestSellerQuantity { get; set; }
}
public class RevenueEnvelope { public List<CashierRevenueView> Data { get; set; } = new(); }
public class CashierRevenueView
{
    public string CashierUsername { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public long TotalPointsAwarded { get; set; }
}
public class TopProductView
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public long TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}
