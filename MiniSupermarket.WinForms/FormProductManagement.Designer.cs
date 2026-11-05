namespace MiniSupermarket.WinForms
{
    partial class FormProductManagement
    {
        private DataGridView dgvProducts = null!;
        private TextBox txtSearchBarcode = null!, txtId = null!, txtBarcode = null!, txtProductName = null!;
        private NumericUpDown nudPrice = null!, nudStock = null!;
        private ComboBox cboCategory = null!, cboFilterCategory = null!;
        private PagingBar pagingBar = null!;
        private Label lblLowStock = null!;
        private DataGridView dgvBestSellers = null!;

        private void InitializeComponent()
        {
            WorkspaceUi.Configure(this);
            dgvProducts = WorkspaceUi.Grid("dgvProducts");
            WorkspaceUi.Column(dgvProducts, "ProductId", "Mã SP", 45);
            WorkspaceUi.Column(dgvProducts, "Barcode", "Mã vạch", 110);
            WorkspaceUi.Column(dgvProducts, "ProductName", "Tên sản phẩm", 180);
            WorkspaceUi.Column(dgvProducts, "Price", "Đơn giá", 80, "N0");
            WorkspaceUi.Column(dgvProducts, "StockQuantity", "Tồn kho", 55, "N0");
            WorkspaceUi.Column(dgvProducts, "CategoryName", "Nhóm hàng", 110);
            dgvProducts.CellClick += dgvProducts_CellClick;
            dgvProducts.CellFormatting += dgvProducts_CellFormatting;
            txtSearchBarcode = new TextBox { Name = "txtSearchBarcode", Width = 230, PlaceholderText = "Mã vạch hoặc tên sản phẩm" };
            cboFilterCategory = new ComboBox { Name = "cboFilterCategory", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterCategory.SelectionChangeCommitted += btnSearch_Click;
            txtSearchBarcode.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; btnSearch_Click(this, EventArgs.Empty); } };
            var toolbar = WorkspaceUi.Toolbar();
            toolbar.Controls.AddRange(new Control[] { txtSearchBarcode, cboFilterCategory,
                WorkspaceUi.Button("btnSearch", "Tìm kiếm", btnSearch_Click), WorkspaceUi.Button("btnLoad", "Tải lại", btnLoad_Click) });
            lblLowStock = new Label { Name = "lblLowStock", Text = "Tồn ≤ 10: đang tải…", ForeColor = Color.Firebrick, AutoSize = true, Margin = new Padding(4, 10, 8, 0) };
            toolbar.Controls.Add(WorkspaceUi.Button("btnExportProducts", "Xuất CSV toàn kho", ExportProducts));
            toolbar.Controls.Add(lblLowStock);
            txtId = new TextBox { Name = "txtId", ReadOnly = true };
            txtBarcode = new TextBox { Name = "txtBarcode", MaxLength = 50 };
            txtProductName = new TextBox { Name = "txtProductName", MaxLength = 150 };
            nudPrice = new NumericUpDown { Name = "nudPrice", Minimum = 0.01m, Value = 0.01m, Maximum = 9999999999999999.99m, DecimalPlaces = 2, ThousandsSeparator = true };
            nudStock = new NumericUpDown { Name = "nudStock", Maximum = int.MaxValue, ThousandsSeparator = true };
            cboCategory = new ComboBox { Name = "cboCategory", DropDownStyle = ComboBoxStyle.DropDownList };
            var editor = WorkspaceUi.Editor();
            WorkspaceUi.Field(editor, "Mã sản phẩm", txtId);
            WorkspaceUi.Field(editor, "Mã vạch", txtBarcode);
            WorkspaceUi.Field(editor, "Tên sản phẩm", txtProductName);
            WorkspaceUi.Field(editor, "Đơn giá (đ)", nudPrice);
            WorkspaceUi.Field(editor, "Số lượng tồn", nudStock);
            WorkspaceUi.Field(editor, "Nhóm hàng", cboCategory);
            var actions = new FlowLayoutPanel { AutoSize = true, Width = 260, MaximumSize = new Size(260, 0) };
            actions.Controls.AddRange(new Control[] {
                WorkspaceUi.Button("btnAdd", "Thêm mới", btnAdd_Click), WorkspaceUi.Button("btnUpdate", "Cập nhật", btnUpdate_Click),
                WorkspaceUi.Button("btnDelete", "Xóa", btnDelete_Click), WorkspaceUi.Button("btnClear", "Làm mới ô nhập", (_, _) => ClearInputs()) });
            editor.Controls.Add(actions);
            dgvBestSellers = WorkspaceUi.Grid("dgvBestSellers");
            WorkspaceUi.Column(dgvBestSellers, "ProductName", "Mặt hàng", 180);
            WorkspaceUi.Column(dgvBestSellers, "TotalQuantity", "Đã bán", 65, "N0");
            var productArea = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = Padding.Empty };
            productArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            productArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            productArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 185));
            productArea.Controls.Add(dgvProducts, 0, 0);
            productArea.Controls.Add(new Label { Text = "TOP 5 BÁN CHẠY · TẤT CẢ NGÀY", Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0), Font = new Font(Font, FontStyle.Bold) }, 0, 1);
            productArea.Controls.Add(dgvBestSellers, 0, 2);
            pagingBar = new PagingBar();
            pagingBar.PageRequested += async page => await LoadProductsAsync(page);
            WorkspaceUi.Mount(this, toolbar, productArea, editor, pagingBar);
            Load += FormProductManagement_Load;
        }
    }
}
