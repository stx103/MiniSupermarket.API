namespace MiniSupermarket.WinForms
{
    partial class FormUserManagement
    {
        private DataGridView dgvUsers = null!;
        private TextBox txtUsername = null!, txtFullName = null!, txtEmail = null!, txtPassword = null!;
        private ComboBox cboRole = null!;
        private Button btnToggleLock = null!;

        private void InitializeComponent()
        {
            WorkspaceUi.Configure(this);
            dgvUsers = WorkspaceUi.Grid("dgvUsers");
            WorkspaceUi.Column(dgvUsers, "Username", "Tài khoản", 100);
            WorkspaceUi.Column(dgvUsers, "FullName", "Họ tên", 140);
            WorkspaceUi.Column(dgvUsers, "Email", "Email", 150);
            WorkspaceUi.Column(dgvUsers, "Role", "Vai trò", 85);
            dgvUsers.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsActive", DataPropertyName = "IsActive", HeaderText = "Hoạt động", FillWeight = 65 });
            dgvUsers.CellClick += dgvUsers_CellClick;
            var toolbar = WorkspaceUi.Toolbar();
            toolbar.Controls.Add(WorkspaceUi.Button("btnLoad", "Tải lại", async (_, _) => await ReloadAsync()));
            txtUsername = new TextBox { Name = "txtUsername", MaxLength = 50 };
            txtFullName = new TextBox { Name = "txtFullName", MaxLength = 100 };
            txtEmail = new TextBox { Name = "txtEmail", MaxLength = 254 };
            txtPassword = new TextBox { Name = "txtPassword", MaxLength = 100, UseSystemPasswordChar = true };
            cboRole = new ComboBox { Name = "cboRole", DropDownStyle = ComboBoxStyle.DropDownList };
            cboRole.Items.AddRange(new object[] { "Admin", "Cashier", "Warehouse" });
            cboRole.SelectedIndex = 1;
            var editor = WorkspaceUi.Editor();
            WorkspaceUi.Field(editor, "Tên đăng nhập", txtUsername);
            WorkspaceUi.Field(editor, "Họ tên nhân viên", txtFullName);
            WorkspaceUi.Field(editor, "Email", txtEmail);
            WorkspaceUi.Field(editor, "Mật khẩu", txtPassword);
            WorkspaceUi.Field(editor, "Vai trò", cboRole);
            var actions = new FlowLayoutPanel { AutoSize = true, Width = 260, MaximumSize = new Size(260, 0) };
            btnToggleLock = WorkspaceUi.Button("btnToggleLock", "Khóa / Mở khóa", btnToggleLock_Click);
            actions.Controls.AddRange(new Control[] {
                WorkspaceUi.Button("btnAddUser", "Tạo tài khoản", btnAddUser_Click),
                WorkspaceUi.Button("btnResetPassword", "Đặt lại mật khẩu", btnResetPassword_Click), btnToggleLock,
                WorkspaceUi.Button("btnClear", "Làm mới ô nhập", (_, _) => ClearInputs()) });
            editor.Controls.Add(actions);
            WorkspaceUi.Mount(this, toolbar, dgvUsers, editor);
            Load += async (_, _) => await ReloadAsync();
        }
    }
}
