namespace MiniSupermarket.WinForms
{
    partial class FormMainShell
    {
        private Panel panelSidebar = null!, panelTopHeader = null!, panelMainContent = null!;
        private Label lblTitle = null!, lblUserInfo = null!;
        private Button btnPOS = null!, btnCategory = null!, btnProduct = null!, btnCustomer = null!, btnReports = null!, btnUserManage = null!, btnLogout = null!;

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Tạp hóa Tiện lợi Minh Khang - Bán lẻ và Tồn kho";
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(1280, 720);
            MinimumSize = new Size(1120, 700);
            StartPosition = FormStartPosition.CenterScreen;
            panelSidebar = new Panel { Name = "panelSidebar", Dock = DockStyle.Left, Width = 230, BackColor = Color.FromArgb(24, 30, 48) };
            var logo = new Label
            {
                Text = "MINH KHANG\nTạp hóa tiện lợi", Dock = DockStyle.Top, Height = 90,
                ForeColor = Color.White, Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Padding = new Padding(16, 12, 0, 0)
            };
            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, Padding = new Padding(8)
            };
            btnPOS = MenuButton("btnPOS", "Bán hàng (POS)", btnPOS_Click);
            btnCategory = MenuButton("btnCategory", "Quản lý danh mục", btnCategory_Click);
            btnProduct = MenuButton("btnProduct", "Sản phẩm và tồn kho", btnProduct_Click);
            btnCustomer = MenuButton("btnCustomer", "Khách hàng thành viên", btnCustomer_Click);
            btnReports = MenuButton("btnReports", "Báo cáo doanh thu", btnReports_Click);
            btnUserManage = MenuButton("btnUserManage", "Quản trị tài khoản", btnUserManage_Click);
            menu.Controls.AddRange(new Control[] { btnPOS, btnCategory, btnProduct, btnCustomer, btnReports, btnUserManage });
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(8) };
            btnLogout = MenuButton("btnLogout", "Đăng xuất", btnLogout_Click);
            btnLogout.Dock = DockStyle.Fill;
            footer.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(menu);
            panelSidebar.Controls.Add(footer);
            panelSidebar.Controls.Add(logo);
            var workspace = new Panel { Dock = DockStyle.Fill };
            panelTopHeader = new Panel { Name = "panelTopHeader", Dock = DockStyle.Top, Height = 82, BackColor = Color.White, Padding = new Padding(16, 8, 16, 8) };
            lblTitle = new Label { Name = "lblTitle", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            lblUserInfo = new Label { Name = "lblUserInfo", Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft };
            panelTopHeader.Controls.Add(lblTitle);
            panelTopHeader.Controls.Add(lblUserInfo);
            panelMainContent = new Panel { Name = "panelMainContent", Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 245, 247) };
            workspace.Controls.Add(panelMainContent);
            workspace.Controls.Add(panelTopHeader);
            Controls.Add(workspace);
            Controls.Add(panelSidebar);
            Load += FormMainShell_Load;
            FormClosing += FormMainShell_FormClosing;
            ResumeLayout(true);
        }

        private static Button MenuButton(string name, string text, EventHandler click)
        {
            var button = new Button
            {
                Name = name, Text = text, Width = 208, Height = 45, Margin = new Padding(0, 2, 0, 2),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0), BackColor = Color.FromArgb(24, 30, 48)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += click;
            return button;
        }
    }
}
