using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MiniSupermarket.WinForms
{
    public partial class FormCustomerManagement : Form
    {
        private readonly PagingBar _pagingBar;
        private int _currentPage = 1;
        private string _keyword = string.Empty;
        private bool _loading;
        public FormCustomerManagement()
        {
            InitializeComponent();
            _pagingBar = new PagingBar();
            dgvCustomers.Dock = DockStyle.Fill;
            grpCustomerList.Controls.Add(_pagingBar);
            _pagingBar.PageRequested += async page => await LoadDataAsync(page);
            WorkspaceUi.ApplyRolePermissions(this, "Customer");
        }

        private async void FormCustomerManagement_Load(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private HttpClient GetAuthenticatedClient()
        {
            return ApiClientService.CreateAuthenticatedClient();
        }

        private async Task LoadDataAsync(int? page = null)
        {
            if (_loading || !WorkspaceUi.ApplyRolePermissions(this, "Customer")) return;
            _loading = true;
            Enabled = false;
            try
            {
                var result = await ApiClientService.GetAsync<PagedViewResult<CustomerDto>>(
                    $"customers/paged?keyword={Uri.EscapeDataString(_keyword)}&pageNumber={page ?? _currentPage}&pageSize={_pagingBar.PageSize}");
                if (IsDisposed) return;
                _currentPage = result.CurrentPage;
                dgvCustomers.DataSource = result.Items;
                FormatDataGridView();
                _pagingBar.Render(result.CurrentPage, result.TotalPages, result.TotalItems);
                ClearInputs();
                dgvCustomers.ClearSelection();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (IsDisposed) return;
                dgvCustomers.DataSource = new List<CustomerDto>();
                FormatDataGridView();
                _currentPage = 1;
                _pagingBar.Render(1, 0, 0);
                ClearInputs();
                WorkspaceUi.Error(ex);
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                WorkspaceUi.Error(ex);
            }
            finally { _loading = false; if (!IsDisposed) Enabled = true; }
        }

        private void FormatDataGridView()
        {
            if (dgvCustomers.Columns.Count == 0)
                return;

            dgvCustomers.AutoGenerateColumns = true;
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvCustomers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvCustomers.RowHeadersVisible = false;
            dgvCustomers.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
            dgvCustomers.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvCustomers.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvCustomers.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvCustomers.DefaultCellStyle.Padding = new Padding(4, 8, 4, 8);
            dgvCustomers.RowTemplate.MinimumHeight = 64;
            foreach (DataGridViewRow row in dgvCustomers.Rows) row.MinimumHeight = 64;

            if (dgvCustomers.Columns["CustomerId"] != null)
            {
                dgvCustomers.Columns["CustomerId"].HeaderText = "Mã KH";
                dgvCustomers.Columns["CustomerId"].MinimumWidth = 38;
                dgvCustomers.Columns["CustomerId"].FillWeight = 40;
            }

            if (dgvCustomers.Columns["CustomerName"] != null)
            {
                dgvCustomers.Columns["CustomerName"].HeaderText = "Tên khách hàng";
                dgvCustomers.Columns["CustomerName"].MinimumWidth = 95;
                dgvCustomers.Columns["CustomerName"].FillWeight = 125;
            }

            if (dgvCustomers.Columns["PhoneNumber"] != null)
            {
                dgvCustomers.Columns["PhoneNumber"].HeaderText = "Số điện thoại";
                dgvCustomers.Columns["PhoneNumber"].MinimumWidth = 90;
                dgvCustomers.Columns["PhoneNumber"].FillWeight = 105;
            }

            if (dgvCustomers.Columns["Address"] != null)
            {
                dgvCustomers.Columns["Address"].HeaderText = "Địa chỉ";
                dgvCustomers.Columns["Address"].MinimumWidth = 160;
                dgvCustomers.Columns["Address"].FillWeight = 300;
            }

            if (dgvCustomers.Columns["RewardPoints"] != null)
            {
                dgvCustomers.Columns["RewardPoints"].HeaderText = "Điểm";
                dgvCustomers.Columns["RewardPoints"].MinimumWidth = 40;
                dgvCustomers.Columns["RewardPoints"].FillWeight = 55;
            }

            if (dgvCustomers.Columns["MembershipRank"] != null)
            {
                dgvCustomers.Columns["MembershipRank"].HeaderText = "Hạng thẻ";
                dgvCustomers.Columns["MembershipRank"].MinimumWidth = 65;
                dgvCustomers.Columns["MembershipRank"].FillWeight = 80;
            }

            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.MultiSelect = false;
            dgvCustomers.AllowUserToAddRows = false;
            dgvCustomers.ReadOnly = true;
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCustomers.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            txtKeyword.Clear();
            _keyword = string.Empty;
            ClearInputs();
            await LoadDataAsync(1);
        }

        private void dgvCustomers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvCustomers.Rows[e.RowIndex];

            txtCustomerId.Text = row.Cells["CustomerId"].Value?.ToString() ?? string.Empty;
            txtCustomerName.Text = row.Cells["CustomerName"].Value?.ToString() ?? string.Empty;
            txtPhoneNumber.Text = row.Cells["PhoneNumber"].Value?.ToString() ?? string.Empty;
            txtAddress.Text = row.Cells["Address"].Value?.ToString() ?? string.Empty;
            txtRewardPoints.Text = row.Cells["RewardPoints"].Value?.ToString() ?? "0";
            txtMembershipRank.Text = row.Cells["MembershipRank"].Value?.ToString() ?? "Chuẩn";
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Customer")) return;
            if (!TryBuildCustomerPayload(out var payload, out _))
                return;

            payload.CustomerId = 0;

            try
            {
                using var client = GetAuthenticatedClient();
                using var response = await client.PostAsJsonAsync("customers", payload);
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Thêm khách hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Customer")) return;
            if (!TryBuildCustomerPayload(out var payload, out int id))
                return;

            if (id <= 0)
            {
                WorkspaceUi.Invalid("Vui lòng chọn khách hàng cần sửa.");
                return;
            }

            try
            {
                using var client = GetAuthenticatedClient();
                using var response = await client.PutAsJsonAsync($"customers/{id}", payload);
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Cập nhật khách hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Customer")) return;
            if (!int.TryParse(txtCustomerId.Text, out int id))
            {
                WorkspaceUi.Invalid("Vui lòng chọn khách hàng cần xóa.");
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa khách hàng ID = {id}?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                using var client = GetAuthenticatedClient();
                using var response = await client.DeleteAsync($"customers/{id}");
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Xóa khách hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Customer")) return;
            _keyword = txtKeyword.Text.Trim();
            await LoadDataAsync(1);
        }

        private bool TryBuildCustomerPayload(out CustomerDto payload, out int id)
        {
            id = 0;
            payload = new CustomerDto();

            if (!string.IsNullOrWhiteSpace(txtCustomerId.Text)
                && !int.TryParse(txtCustomerId.Text, out id))
            {
                WorkspaceUi.Invalid("Mã khách hàng không hợp lệ.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                WorkspaceUi.Invalid("Vui lòng nhập tên khách hàng.");
                txtCustomerName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
            {
                WorkspaceUi.Invalid("Vui lòng nhập số điện thoại.");
                txtPhoneNumber.Focus();
                return false;
            }

            var addressParts = txtAddress.Text.Trim().Split(',');
            if (addressParts.Length != 4 || addressParts.Any(string.IsNullOrWhiteSpace))
            {
                WorkspaceUi.Invalid("Địa chỉ phải đủ 4 phần: số nhà, tên đường, phường/xã, tỉnh/thành phố; ngăn cách bằng dấu phẩy.");
                txtAddress.Focus();
                return false;
            }

            if (!int.TryParse(txtRewardPoints.Text, out int rewardPoints) || rewardPoints < 0)
            {
                WorkspaceUi.Invalid("Điểm tích lũy phải là số nguyên không âm.");
                txtRewardPoints.Focus();
                return false;
            }

            payload = new CustomerDto
            {
                CustomerId = id,
                CustomerName = txtCustomerName.Text.Trim(),
                PhoneNumber = txtPhoneNumber.Text.Trim(),
                Address = txtAddress.Text.Trim(),
                RewardPoints = rewardPoints,
                MembershipRank = string.IsNullOrWhiteSpace(txtMembershipRank.Text)
                    ? "Chuẩn"
                    : txtMembershipRank.Text.Trim()
            };

            return true;
        }

        private void ClearInputs()
        {
            txtCustomerId.Clear();
            txtCustomerName.Clear();
            txtPhoneNumber.Clear();
            txtAddress.Clear();
            txtRewardPoints.Text = "0";
            txtMembershipRank.Text = "Chuẩn";
        }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        public int RewardPoints { get; set; }

        public string MembershipRank { get; set; } = "Chuẩn";
    }
}
