using System.Net.Http.Json;

namespace MiniSupermarket.WinForms
{
    public partial class FormProductManagement : Form
    {
        private int _currentPage = 1;
        private string _keyword = string.Empty;
        private int? _categoryId;
        private bool _loading;
        public FormProductManagement()
        {
            InitializeComponent();
            WorkspaceUi.ApplyRolePermissions(this, "Product");
        }

        private async void FormProductManagement_Load(object? sender, EventArgs e)
        {
            try { await LoadCategoriesAsync(); await LoadProductsAsync(); }
            catch (Exception ex) { if (!IsDisposed) WorkspaceUi.Error(ex); }
        }

        private async Task LoadCategoriesAsync()
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Product")) return;
            var categories = await ApiClientService.GetAsync<List<CategoryDto>>("categories");
            if (IsDisposed) return;
            cboCategory.DisplayMember = cboFilterCategory.DisplayMember = "CategoryName";
            cboCategory.ValueMember = cboFilterCategory.ValueMember = "CategoryId";
            cboCategory.DataSource = categories.ToList();
            cboFilterCategory.DataSource = new[] { new CategoryDto { CategoryId = 0, CategoryName = "Tất cả nhóm hàng" } }.Concat(categories).ToList();
        }

        private async Task LoadProductsAsync(int? page = null)
        {
            if (_loading || !WorkspaceUi.ApplyRolePermissions(this, "Product")) return;
            _loading = true;
            Enabled = false;
            try
            {
                var category = _categoryId.HasValue ? $"&categoryId={_categoryId}" : string.Empty;
                var rows = await ApiClientService.GetAsync<PagedViewResult<ProductDto>>(
                    $"products/paged?keyword={Uri.EscapeDataString(_keyword)}&pageNumber={page ?? _currentPage}&pageSize={pagingBar.PageSize}{category}");
                if (IsDisposed) return;
                _currentPage = rows.CurrentPage;
                dgvProducts.DataSource = rows.Items;
                pagingBar.Render(rows.CurrentPage, rows.TotalPages, rows.TotalItems);
                var low = await ApiClientService.GetAsync<List<ProductDto>>("ado-products/filter-stock?minStock=10");
                var top = await ApiClientService.GetAsync<List<TopProductView>>("reports/top-selling");
                if (IsDisposed) return;
                lblLowStock.Text = $"● {low.Count} mặt hàng tồn ≤ 10 (toàn kho)";
                dgvBestSellers.DataSource = top;
                ClearInputs();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (IsDisposed) return;
                dgvProducts.DataSource = new List<ProductDto>();
                _currentPage = 1;
                pagingBar.Render(1, 0, 0);
                ClearInputs();
                WorkspaceUi.Error(ex);
            }
            catch (Exception ex) { if (!IsDisposed) WorkspaceUi.Error(ex); }
            finally { _loading = false; if (!IsDisposed) Enabled = true; }
        }

        private void dgvProducts_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvProducts.Rows[e.RowIndex].DataBoundItem is not ProductDto p) return;
            txtId.Text = p.ProductId.ToString();
            txtBarcode.Text = p.Barcode;
            txtProductName.Text = p.ProductName;
            nudPrice.Value = Math.Clamp(p.Price, nudPrice.Minimum, nudPrice.Maximum);
            nudStock.Value = p.StockQuantity;
            cboCategory.SelectedValue = p.CategoryId;
        }

        private async void btnSearch_Click(object? sender, EventArgs e)
        {
            _keyword = txtSearchBarcode.Text.Trim();
            _categoryId = cboFilterCategory.SelectedValue is int id && id > 0 ? id : null;
            await LoadProductsAsync(1);
        }

        private async void btnLoad_Click(object? sender, EventArgs e)
        {
            try
            {
                txtSearchBarcode.Clear();
                _keyword = string.Empty;
                _categoryId = null;
                await LoadCategoriesAsync();
                ClearInputs();
                await LoadProductsAsync(1);
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private async void btnAdd_Click(object? sender, EventArgs e) => await SaveAsync(false);
        private async void btnUpdate_Click(object? sender, EventArgs e) => await SaveAsync(true);

        private async Task SaveAsync(bool update)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Product")) return;
            if (update && !int.TryParse(txtId.Text, out _)) { WorkspaceUi.Invalid("Chọn sản phẩm cần sửa."); return; }
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) || string.IsNullOrWhiteSpace(txtProductName.Text) || cboCategory.SelectedValue is not int)
            { WorkspaceUi.Invalid("Nhập mã vạch, tên sản phẩm và chọn nhóm hàng."); return; }
            var payload = new
            {
                Barcode = txtBarcode.Text.Trim(), ProductName = txtProductName.Text.Trim(), Price = nudPrice.Value,
                StockQuantity = (int)nudStock.Value, CategoryId = (int)cboCategory.SelectedValue
            };
            try
            {
                using var response = update
                    ? await ApiClientService.Client.PutAsJsonAsync($"products/{txtId.Text}", payload)
                    : await ApiClientService.Client.PostAsJsonAsync("products", payload);
                await ApiClientService.EnsureSuccessAsync(response);
                await LoadProductsAsync();
                ClearInputs();
                WorkspaceUi.Success(response, update ? "Cập nhật sản phẩm thành công." : "Thêm sản phẩm thành công.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private async void btnDelete_Click(object? sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Product")) return;
            if (!int.TryParse(txtId.Text, out int id)) { WorkspaceUi.Invalid("Chọn sản phẩm cần xóa."); return; }
            if (MessageBox.Show($"Xóa sản phẩm {txtProductName.Text}?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                using var response = await ApiClientService.Client.DeleteAsync($"products/{id}");
                await ApiClientService.EnsureSuccessAsync(response);
                await LoadProductsAsync();
                ClearInputs();
                WorkspaceUi.Success(response, "Xóa sản phẩm thành công.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private void ClearInputs()
        {
            txtId.Clear(); txtBarcode.Clear(); txtProductName.Clear();
            nudPrice.Value = nudPrice.Minimum;
            nudStock.Value = 0;
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;
            dgvProducts.ClearSelection();
        }

        // Cảnh báo theo yêu cầu: cả hàng có tồn <=10 được tô LightCoral trong CellFormatting.
        private void dgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvProducts.Rows[e.RowIndex].DataBoundItem is ProductDto product && product.StockQuantity <= 10)
            {
                e.CellStyle!.BackColor = Color.LightCoral;
                e.CellStyle.SelectionBackColor = Color.Firebrick;
                e.CellStyle.SelectionForeColor = Color.White;
            }
        }

        private async void ExportProducts(object? sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Product")) return;
            try
            {
                // Lấy toàn bộ danh mục từ API, không xuất chỉ 5/20 dòng của trang hiện tại.
                var all = await ApiClientService.GetAsync<List<ProductDto>>("products");
                CsvExport.Save(this, $"Ton_kho_{DateTime.Today:yyyyMMdd}.csv",
                    new[] { "Mã SP", "Mã vạch", "Tên sản phẩm", "Đơn giá", "Tồn kho", "Nhóm hàng" },
                    all.Select(p => new object?[] { p.ProductId, p.Barcode, p.ProductName, p.Price, p.StockQuantity, p.CategoryName }));
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }
    }

    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
