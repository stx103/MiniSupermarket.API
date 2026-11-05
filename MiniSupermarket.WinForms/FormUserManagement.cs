using System.Net.Http.Json;

namespace MiniSupermarket.WinForms
{
    public partial class FormUserManagement : Form
    {
        private UserDto? _selected;
        public FormUserManagement()
        {
            InitializeComponent();
            WorkspaceUi.ApplyRolePermissions(this, "Users");
        }

        private async Task ReloadAsync()
        {
            if (!WorkspaceUi.ApplyRolePermissions(this, "Users")) return;
            try
            {
                var users = await ApiClientService.GetAsync<List<UserDto>>("users");
                if (IsDisposed) return;
                dgvUsers.DataSource = users;
                ClearInputs();
            }
            catch (Exception ex) { if (!IsDisposed) WorkspaceUi.Error(ex); }
        }

        private void dgvUsers_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvUsers.Rows[e.RowIndex].DataBoundItem is not UserDto user) return;
            _selected = user;
            txtUsername.Text = user.Username;
            txtFullName.Text = user.FullName;
            txtEmail.Text = user.Email;
            txtPassword.Clear();
            cboRole.SelectedItem = user.Role;
            btnToggleLock.Text = user.IsActive ? "Khóa tài khoản" : "Mở khóa tài khoản";
        }

        private bool CheckAdmin()
        {
            if (WorkspaceUi.ApplyRolePermissions(this, "Users")) return true;
            MessageBox.Show("403 - Chỉ Admin được quản trị tài khoản.", "Từ chối");
            return false;
        }

        private async void btnAddUser_Click(object? sender, EventArgs e)
        {
            if (!CheckAdmin()) return;
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text) || txtPassword.Text.Length < 6)
            { WorkspaceUi.Invalid("Nhập tên đăng nhập, họ tên và mật khẩu từ 6 ký tự."); return; }
            try
            {
                using var response = await ApiClientService.Client.PostAsJsonAsync("users", new
                {
                    Username = txtUsername.Text.Trim(), FullName = txtFullName.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    Password = txtPassword.Text, Role = cboRole.SelectedItem!.ToString()
                });
                await ApiClientService.EnsureSuccessAsync(response);
                await ReloadAsync();
                WorkspaceUi.Success(response, "Đã tạo tài khoản.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private async void btnResetPassword_Click(object? sender, EventArgs e)
        {
            if (!CheckAdmin()) return;
            if (_selected == null) { WorkspaceUi.Invalid("Chọn tài khoản cần đặt lại mật khẩu."); return; }
            if (txtPassword.Text.Length < 6) { WorkspaceUi.Invalid("Nhập mật khẩu mới từ 6 ký tự."); return; }
            if (MessageBox.Show($"Đặt lại mật khẩu cho {_selected.Username}?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                using var response = await ApiClientService.Client.PutAsJsonAsync($"users/{_selected.UserId}/password", new { Password = txtPassword.Text });
                await ApiClientService.EnsureSuccessAsync(response);
                txtPassword.Clear();
                WorkspaceUi.Success(response, "Đã đặt lại mật khẩu.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private async void btnToggleLock_Click(object? sender, EventArgs e)
        {
            if (!CheckAdmin()) return;
            if (_selected == null) { WorkspaceUi.Invalid("Chọn tài khoản cần khóa hoặc mở khóa."); return; }
            if (MessageBox.Show($"{(_selected.IsActive ? "Khóa" : "Mở khóa")} tài khoản {_selected.Username}?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                using var response = await ApiClientService.Client.PutAsJsonAsync($"users/{_selected.UserId}/status", new { IsActive = !_selected.IsActive });
                await ApiClientService.EnsureSuccessAsync(response);
                await ReloadAsync();
                WorkspaceUi.Success(response, "Đã cập nhật trạng thái tài khoản.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
        }

        private void ClearInputs()
        {
            _selected = null;
            txtUsername.Clear(); txtFullName.Clear(); txtEmail.Clear(); txtPassword.Clear();
            cboRole.SelectedIndex = 1;
            btnToggleLock.Text = "Khóa / Mở khóa";
            dgvUsers.ClearSelection();
        }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
