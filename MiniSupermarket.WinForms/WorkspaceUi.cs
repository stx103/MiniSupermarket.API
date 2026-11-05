namespace MiniSupermarket.WinForms
{
    internal static class WorkspaceUi
    {
        public static bool ApplyRolePermissions(Form form, string screen)
        {
            bool allowed = SessionManager.CanAccess(screen);
            foreach (Control control in form.Controls) control.Enabled = allowed;
            return allowed;
        }

        public static void Configure(Form form)
        {
            form.Font = new Font("Segoe UI", 9F);
            form.BackColor = Color.White;
            form.ClientSize = new Size(1040, 640);
            form.AutoScaleMode = AutoScaleMode.Font;
        }

        public static DataGridView Grid(string name) => new()
        {
            Name = name, Dock = DockStyle.Fill, BackgroundColor = Color.White,
            AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            ReadOnly = true, MultiSelect = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowTemplate = { Height = 30 }
        };

        public static void Column(DataGridView grid, string property, string title, float weight = 100, string? format = null)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = property, DataPropertyName = property, HeaderText = title,
                FillWeight = weight, MinimumWidth = 50,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format ?? string.Empty }
            });
        }

        public static Button Button(string name, string text, EventHandler click)
        {
            var button = new Button { Name = name, Text = text, AutoSize = true, MinimumSize = new Size(96, 34), Margin = new Padding(0, 4, 8, 4) };
            button.Click += click;
            return button;
        }

        public static FlowLayoutPanel Toolbar() => new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 0, 0, 8), WrapContents = true };

        public static FlowLayoutPanel Editor() => new()
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, Padding = new Padding(16, 0, 0, 0)
        };

        public static void Field(FlowLayoutPanel editor, string title, Control control)
        {
            editor.Controls.Add(new Label { Text = title, AutoSize = false, Width = 260, Height = 22, Margin = new Padding(0, 6, 0, 0) });
            control.Width = 260;
            control.Margin = new Padding(0, 0, 0, 4);
            editor.Controls.Add(control);
        }

        public static void Mount(Form form, Control toolbar, Control grid, Control editor, Control? footer = null)
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(16) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            var list = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            list.Controls.Add(grid);
            if (footer != null) list.Controls.Add(footer);
            content.Controls.Add(list, 0, 0);
            content.Controls.Add(editor, 1, 0);
            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(content, 0, 1);
            form.Controls.Add(root);
        }

        public static void Error(Exception ex) => MessageBox.Show(ApiFeedback.ErrorMessage(ex), "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static void Success(HttpResponseMessage response, string message) => MessageBox.Show(
            ApiFeedback.Format(response.StatusCode, message), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void Invalid(string message) => MessageBox.Show(
            ApiFeedback.Format(System.Net.HttpStatusCode.BadRequest, message), "Kiểm tra dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
