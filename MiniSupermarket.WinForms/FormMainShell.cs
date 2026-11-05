namespace MiniSupermarket.WinForms
{
    public partial class FormMainShell : Form
    {
        private Form? _activeForm;
        public FormMainShell() { InitializeComponent(); DoubleBuffered = true; }

        private void FormMainShell_Load(object? sender, EventArgs e)
        {
            lblUserInfo.Text = $"Nhân viên: {SessionManager.CurrentUsername} | {SessionManager.CurrentFullName} | Vai trò: {SessionManager.CurrentRole}";
            ApplyRolePermissions();
            switch (SessionManager.CurrentRole)
            {
                case "Admin": btnCategory_Click(this, EventArgs.Empty); break;
                case "Cashier": btnPOS_Click(this, EventArgs.Empty); break;
                case "Warehouse": btnProduct_Click(this, EventArgs.Empty); break;
                default:
                    MessageBox.Show("403 - Tài khoản chưa được cấp vai trò hợp lệ.", "Từ chối truy cập");
                    DialogResult = DialogResult.Retry;
                    Close();
                    break;
            }
        }

        private void ApplyRolePermissions()
        {
            btnPOS.Visible = SessionManager.CanAccess("POS");
            btnCategory.Visible = SessionManager.CanAccess("Category");
            btnProduct.Visible = SessionManager.CanAccess("Product");
            btnCustomer.Visible = SessionManager.CanAccess("Customer");
            btnReports.Visible = SessionManager.CanAccess("Reports");
            btnUserManage.Visible = SessionManager.CanAccess("Users");
        }

        private void OpenChildForm<T>(string screen, string title, Button button) where T : Form, new()
        {
            if (!SessionManager.CanAccess(screen))
            {
                MessageBox.Show("403 - Bạn không có quyền truy cập chức năng này.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            if (_activeForm is T) return;
            if (!CanLeaveCurrentScreen()) return;
            panelMainContent.SuspendLayout();
            try
            {
            _activeForm?.Close();
            _activeForm?.Dispose();
            panelMainContent.Controls.Clear();
            var child = new T { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
            _activeForm = child;
            panelMainContent.Controls.Add(child);
            panelMainContent.Tag = child;
            lblTitle.Text = title;
            foreach (var nav in new[] { btnPOS, btnCategory, btnProduct, btnCustomer, btnReports, btnUserManage })
                nav.BackColor = nav == button ? Color.FromArgb(41, 100, 180) : Color.FromArgb(24, 30, 48);
            child.Show();
            }
            finally { panelMainContent.ResumeLayout(true); }
        }

        private bool CanLeaveCurrentScreen()
        {
            if (_activeForm is not FormPOS pos) return true;
            if (pos.IsBusy) { MessageBox.Show("Vui lòng đợi giao dịch đang xử lý hoàn tất."); return false; }
            return !pos.HasCart || MessageBox.Show("Giỏ hàng chưa thanh toán sẽ bị hủy. Tiếp tục?", "Giỏ hàng đang mở", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public void OpenCustomerScreen() => btnCustomer_Click(this, EventArgs.Empty);
        private void btnCategory_Click(object? sender, EventArgs e) => OpenChildForm<FormCategoryManagement>("Category", "QUẢN LÝ DANH MỤC NHÓM HÀNG", btnCategory);
        private void btnCustomer_Click(object? sender, EventArgs e) => OpenChildForm<FormCustomerManagement>("Customer", "KHÁCH HÀNG VÀ TÍCH ĐIỂM", btnCustomer);
        private void btnProduct_Click(object? sender, EventArgs e) => OpenChildForm<FormProductManagement>("Product", "SẢN PHẨM VÀ TỒN KHO", btnProduct);
        private void btnPOS_Click(object? sender, EventArgs e) => OpenChildForm<FormPOS>("POS", "BÁN HÀNG TẠI QUẦY", btnPOS);
        private void btnReports_Click(object? sender, EventArgs e) => OpenChildForm<FormQuickReport>("Reports", "BÁO CÁO DOANH THU VÀ HIỆU SUẤT", btnReports);
        private void btnUserManage_Click(object? sender, EventArgs e) => OpenChildForm<FormUserManagement>("Users", "QUẢN TRỊ TÀI KHOẢN VÀ PHÂN QUYỀN", btnUserManage);

        private void btnLogout_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Đăng xuất phiên làm việc hiện tại?", "Đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            DialogResult = DialogResult.Retry;
            Close();
        }

        private void FormMainShell_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!CanLeaveCurrentScreen())
            {
                e.Cancel = true;
                DialogResult = DialogResult.None;
                return;
            }
            _activeForm?.Dispose();
            SessionManager.Clear();
        }
    }
}
