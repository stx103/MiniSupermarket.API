#nullable enable
using System.Windows.Forms.DataVisualization.Charting;
namespace MiniSupermarket.WinForms;

partial class FormQuickReport
{
    private DateTimePicker dtpFromDate = null!, dtpToDate = null!;
    private ComboBox cboPeriod = null!;
    private Button btnRunRange = null!, btnLatestMonth = null!, btnExportRevenue = null!, btnExportInvoices = null!;
    private Label lblTotalOrders = null!, lblTotalRevenue = null!, lblNewCustomers = null!, lblLowStock = null!, lblRangeTotal = null!;
    private DataGridView dgvCashierRevenue = null!, dgvTopProducts = null!, dgvInvoices = null!, dgvDailyRevenue = null!;
    private Chart revenueChart = null!;

    private void InitializeComponent()
    {
        WorkspaceUi.Configure(this);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
        for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        lblTotalRevenue = AddSummary(cards, 0, "DOANH THU HÔM NAY", "lblTotalRevenue", Color.FromArgb(223, 242, 231));
        lblTotalOrders = AddSummary(cards, 1, "HÓA ĐƠN ĐÃ XUẤT", "lblTotalOrders", Color.FromArgb(225, 238, 251));
        lblNewCustomers = AddSummary(cards, 2, "KHÁCH MỚI HÔM NAY", "lblNewCustomers", Color.FromArgb(237, 229, 248));
        lblLowStock = AddSummary(cards, 3, "SẮP HẾT KHO (≤10)", "lblLowStock", Color.FromArgb(255, 225, 218));
        root.Controls.Add(cards, 0, 0);
        var toolbar = WorkspaceUi.Toolbar();
        dtpFromDate = DatePicker("dtpFromDate", DateTime.Today.AddDays(-6)); dtpToDate = DatePicker("dtpToDate", DateTime.Today);
        cboPeriod = new ComboBox { Name = "cboPeriod", DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        cboPeriod.Items.AddRange(new object[] { "Ngày", "Tuần", "Tháng" }); cboPeriod.SelectedIndex = 0;
        btnRunRange = WorkspaceUi.Button("btnRunRange", "Xem báo cáo", async (_, _) => await LoadReportsAsync());
        btnLatestMonth = WorkspaceUi.Button("btnLatestMonth", "Tháng có hóa đơn", async (_, _) => await LoadReportsAsync(true));
        btnExportRevenue = WorkspaceUi.Button("btnExportRevenue", "Xuất CSV doanh thu", ExportRevenue);
        btnExportInvoices = WorkspaceUi.Button("btnExportInvoices", "Xuất CSV hóa đơn", ExportInvoices);
        toolbar.Controls.AddRange(new Control[] { new Label { Text = "Từ", AutoSize = true }, dtpFromDate, new Label { Text = "Đến", AutoSize = true }, dtpToDate, cboPeriod, btnRunRange, btnLatestMonth, btnExportRevenue, btnExportInvoices });
        root.Controls.Add(toolbar, 0, 1);
        lblRangeTotal = new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 10), Text = "Đang tải báo cáo…" }; root.Controls.Add(lblRangeTotal, 0, 2);
        var tabs = new TabControl { Name = "reportTabs", Dock = DockStyle.Fill };
        var chartPage = new TabPage("Biểu đồ doanh thu");
        revenueChart = new Chart { Name = "revenueChart", Dock = DockStyle.Fill, BackColor = Color.White };
        var area = new ChartArea("Revenue"); area.AxisX.Interval = 1; area.AxisX.MajorGrid.Enabled = false;
        area.AxisY.Minimum = 0; area.AxisY.LabelStyle.Format = "N0"; area.AxisY.Title = "VNĐ (sau giảm giá)"; area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
        revenueChart.ChartAreas.Add(area); revenueChart.Titles.Add("Doanh thu thực thu");
        revenueChart.Series.Add(new Series("Thực thu") { ChartType = SeriesChartType.Column, Color = Color.FromArgb(41, 100, 180), ToolTip = "#VALX: #VALY{N0} đ" });
        chartPage.Controls.Add(revenueChart); tabs.TabPages.Add(chartPage);
        dgvDailyRevenue = AddGridTab(tabs, "Theo kỳ", "dgvDailyRevenue");
        WorkspaceUi.Column(dgvDailyRevenue, "PeriodStart", "Bắt đầu kỳ", 110, "dd/MM/yyyy");
        WorkspaceUi.Column(dgvDailyRevenue, "TotalOrders", "Hóa đơn", 70, "N0"); WorkspaceUi.Column(dgvDailyRevenue, "TotalRevenue", "Thực thu (đ)", 120, "N0");
        dgvCashierRevenue = AddGridTab(tabs, "Theo thu ngân", "dgvCashierRevenue");
        WorkspaceUi.Column(dgvCashierRevenue, "CashierUsername", "Thu ngân", 120);
        WorkspaceUi.Column(dgvCashierRevenue, "TotalOrders", "Hóa đơn", 70, "N0"); WorkspaceUi.Column(dgvCashierRevenue, "TotalRevenue", "Thực thu (đ)", 120, "N0");
        WorkspaceUi.Column(dgvCashierRevenue, "TotalPointsAwarded", "Điểm cộng", 80, "N0");
        dgvTopProducts = AddGridTab(tabs, "Top 5 · Tất cả ngày", "dgvTopProducts");
        WorkspaceUi.Column(dgvTopProducts, "ProductName", "Sản phẩm", 160); WorkspaceUi.Column(dgvTopProducts, "TotalQuantity", "Số lượng bán", 80, "N0");
        WorkspaceUi.Column(dgvTopProducts, "TotalRevenue", "Doanh số trước ưu đãi (đ)", 140, "N0");
        dgvInvoices = AddGridTab(tabs, "Hóa đơn", "dgvInvoices");
        WorkspaceUi.Column(dgvInvoices, "OrderCode", "Mã hóa đơn", 180); WorkspaceUi.Column(dgvInvoices, "OrderDate", "Ngày lập", 120, "dd/MM/yyyy HH:mm");
        WorkspaceUi.Column(dgvInvoices, "CustomerName", "Khách hàng", 120); WorkspaceUi.Column(dgvInvoices, "TotalAmount", "Tiền hàng", 90, "N0");
        WorkspaceUi.Column(dgvInvoices, "DiscountAmount", "Giảm giá", 80, "N0"); WorkspaceUi.Column(dgvInvoices, "FinalAmount", "Thực thu", 90, "N0");
        root.Controls.Add(tabs, 0, 3); Controls.Add(root); Load += async (_, _) => await LoadReportsAsync(true);
        void InvalidateExport(object? s, EventArgs e) { btnExportRevenue.Enabled = btnExportInvoices.Enabled = false; }
        dtpFromDate.ValueChanged += InvalidateExport; dtpToDate.ValueChanged += InvalidateExport; cboPeriod.SelectedIndexChanged += InvalidateExport;
    }
    private static DateTimePicker DatePicker(string name, DateTime date) => new() { Name = name, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 115, Value = date };
    private static DataGridView AddGridTab(TabControl tabs, string text, string name)
    { var page = new TabPage(text); var grid = WorkspaceUi.Grid(name); page.Controls.Add(grid); tabs.TabPages.Add(page); return grid; }
    private static Label AddSummary(TableLayoutPanel parent, int column, string title, string name, Color color)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = color, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 10) };
        var value = new Label { Name = name, Text = "…", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 19F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(value); panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        parent.Controls.Add(panel, column, 0); return value;
    }
}
