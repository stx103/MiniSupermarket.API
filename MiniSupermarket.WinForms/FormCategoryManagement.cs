using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace MiniSupermarket.WinForms
{
    // =============================================================
    // FORM QUẢN LÝ NHÓM HÀNG
    // =============================================================
    public partial class FormCategoryManagement : Form
    {
        public FormCategoryManagement()
        {
            InitializeComponent();
            btnCustomers.Visible = false;
            AutoScroll = true;
            AutoScrollMinSize = ClientSize;
            WorkspaceUi.ApplyRolePermissions(this, "Category");
        }

        // Khi Form mở
        private async void FormCategoryManagement_Load(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        // =========================================================
        // TẠO HTTP CLIENT KÈM JWT TOKEN
        // =========================================================
        private HttpClient GetAuthenticatedClient()
        {
            return ApiClientService.CreateAuthenticatedClient();
        }

        // =========================================================
        // LOAD DATA
        // =========================================================
        private async Task LoadDataAsync()
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Category")) return;
            try
            {
                var categories = await ApiClientService.GetAsync<List<CategoryDto>>("categories");
                if (IsDisposed) return;
                dgvCategories.DataSource = categories;

                FormatDataGridView();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        // =========================================================
        // FORMAT DATAGRIDVIEW
        // =========================================================
        private void FormatDataGridView()
        {
            if (dgvCategories.Columns.Count == 0)
                return;

            dgvCategories.AutoGenerateColumns = true;
            dgvCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            if (dgvCategories.Columns["CategoryId"] != null)
            {
                dgvCategories.Columns["CategoryId"].HeaderText = "Mã ID";
                dgvCategories.Columns["CategoryId"].Width = 65;
            }

            if (dgvCategories.Columns["CategoryName"] != null)
            {
                dgvCategories.Columns["CategoryName"].HeaderText = "Tên Nhóm hàng";
                dgvCategories.Columns["CategoryName"].Width = 275;
            }

            if (dgvCategories.Columns["Description"] != null)
            {
                dgvCategories.Columns["Description"].HeaderText = "Mô Tả";
                dgvCategories.Columns["Description"].Width = 260;
            }

            dgvCategories.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvCategories.MultiSelect = false;

            dgvCategories.AllowUserToAddRows = false;

            dgvCategories.ReadOnly = true;
        }

        // =========================================================
        // TẢI LẠI
        // =========================================================
        private async void btnLoad_Click(object sender, EventArgs e)
        {
            txtKeyword.Clear();
            ClearInputs();

            await LoadDataAsync();
        }

        // =========================================================
        // CLICK DÒNG
        // =========================================================
        private void dgvCategories_CellClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvCategories.Rows[e.RowIndex];

            if (row.Cells["CategoryId"].Value != null)
            {
                txtId.Text =
                    row.Cells["CategoryId"].Value.ToString();
            }

            if (row.Cells["CategoryName"].Value != null)
            {
                txtCategoryName.Text =
                    row.Cells["CategoryName"].Value.ToString();
            }

            if (row.Cells["Description"].Value != null)
            {
                txtDescription.Text =
                    row.Cells["Description"].Value.ToString();
            }
            else
            {
                txtDescription.Clear();
            }
        }

        // =========================================================
        // THÊM MỚI
        // =========================================================
        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Category")) return;
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show(
                    "400 - Vui lòng nhập tên nhóm hàng!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtCategoryName.Focus();
                return;
            }

            try
            {
                var newCat = new
                {
                    CategoryName = txtCategoryName.Text.Trim(),
                    Description = txtDescription.Text.Trim()
                };

                using var client = GetAuthenticatedClient();

                using var response =
                    await client.PostAsJsonAsync("categories", newCat);
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Thêm nhóm hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        // =========================================================
        // CẬP NHẬT
        // =========================================================
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Category")) return;
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show(
                    "400 - Vui lòng chọn nhóm hàng cần sửa!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!int.TryParse(txtId.Text, out int id))
            {
                MessageBox.Show(
                    "400 - Mã nhóm hàng không hợp lệ!",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show(
                    "400 - Vui lòng nhập tên nhóm hàng!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                var updateCat = new
                {
                    CategoryId = id,
                    CategoryName = txtCategoryName.Text.Trim(),
                    Description = txtDescription.Text.Trim()
                };

                using var client = GetAuthenticatedClient();

                using var response =
                    await client.PutAsJsonAsync(
                        $"categories/{id}",
                        updateCat);
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Cập nhật nhóm hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        // =========================================================
        // XÓA
        // =========================================================
        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Category")) return;
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show(
                    "400 - Vui lòng chọn nhóm hàng cần xóa!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!int.TryParse(txtId.Text, out int id))
            {
                MessageBox.Show(
                    "400 - Mã nhóm hàng không hợp lệ!",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa nhóm hàng ID = {id}?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                using var client = GetAuthenticatedClient();

                using var response =
                    await client.DeleteAsync($"categories/{id}");
                await ApiClientService.EnsureSuccessAsync(response);
                WorkspaceUi.Success(response, "Xóa nhóm hàng thành công.");
                await LoadDataAsync();
                ClearInputs();
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        // =========================================================
        // TÌM KIẾM
        // =========================================================
        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Category")) return;
            string keyword = txtKeyword.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                await LoadDataAsync();
                return;
            }

            try
            {
                string encodedKeyword =
                    Uri.EscapeDataString(keyword);

                var result = await ApiClientService.GetAsync<List<CategoryDto>>(
                    $"categories/search?keyword={encodedKeyword}");

                dgvCategories.DataSource = result;

                FormatDataGridView();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                dgvCategories.DataSource = new List<CategoryDto>();
                FormatDataGridView();
                ClearInputs();
                WorkspaceUi.Error(ex);
            }
            catch (Exception ex)
            {
                WorkspaceUi.Error(ex);
            }
        }

        // =========================================================
        // XÓA Ô NHẬP
        // =========================================================
        private void ClearInputs()
        {
            txtId.Clear();
            txtCategoryName.Clear();
            txtDescription.Clear();
        }

        private void grpCategoryList_Enter(object sender, EventArgs e)
        {

        }

        private void btnCustomers_Click(object sender, EventArgs e)
        {
            if (FindForm()?.TopLevelControl is FormMainShell shell) shell.OpenCustomerScreen();
        }
    }

    // =============================================================
    // DTO (Data Transfer Object) cho Category
    // =============================================================
    public class CategoryDto
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
