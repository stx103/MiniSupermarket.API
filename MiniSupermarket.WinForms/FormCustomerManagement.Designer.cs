namespace MiniSupermarket.WinForms
{
    partial class FormCustomerManagement
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            dgvCustomers = new DataGridView();
            txtKeyword = new TextBox();
            btnSearch = new Button();
            btnLoad = new Button();
            grpSearch = new GroupBox();
            grpCustomerList = new GroupBox();
            grpCustomerInfo = new GroupBox();
            lblCustomerId = new Label();
            txtCustomerId = new TextBox();
            lblCustomerName = new Label();
            txtCustomerName = new TextBox();
            lblPhoneNumber = new Label();
            txtPhoneNumber = new TextBox();
            lblAddress = new Label();
            txtAddress = new TextBox();
            lblRewardPoints = new Label();
            txtRewardPoints = new TextBox();
            lblMembershipRank = new Label();
            txtMembershipRank = new TextBox();
            btnAdd = new Button();
            btnUpdate = new Button();
            btnDelete = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
            grpSearch.SuspendLayout();
            grpCustomerList.SuspendLayout();
            grpCustomerInfo.SuspendLayout();
            SuspendLayout();
            //
            // dgvCustomers
            //
            dgvCustomers.AllowUserToAddRows = false;
            dgvCustomers.AllowUserToDeleteRows = false;
            dgvCustomers.AllowUserToResizeRows = false;
            dgvCustomers.BackgroundColor = Color.White;
            dgvCustomers.BorderStyle = BorderStyle.Fixed3D;
            dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCustomers.Location = new Point(11, 33);
            dgvCustomers.Margin = new Padding(3, 4, 3, 4);
            dgvCustomers.MultiSelect = false;
            dgvCustomers.Name = "dgvCustomers";
            dgvCustomers.ReadOnly = true;
            dgvCustomers.RowHeadersWidth = 40;
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.Size = new Size(647, 407);
            dgvCustomers.TabIndex = 0;
            dgvCustomers.CellClick += dgvCustomers_CellClick;
            //
            // txtKeyword
            //
            txtKeyword.Location = new Point(17, 29);
            txtKeyword.Margin = new Padding(3, 4, 3, 4);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.PlaceholderText = "Nhập tên, số điện thoại hoặc hạng thẻ...";
            txtKeyword.Size = new Size(502, 27);
            txtKeyword.TabIndex = 1;
            //
            // btnSearch
            //
            btnSearch.Location = new Point(531, 28);
            btnSearch.Margin = new Padding(3, 4, 3, 4);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(114, 33);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Tìm kiếm";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            //
            // btnLoad
            //
            btnLoad.Location = new Point(657, 28);
            btnLoad.Margin = new Padding(3, 4, 3, 4);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(114, 33);
            btnLoad.TabIndex = 3;
            btnLoad.Text = "Tải lại";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            //
            // grpSearch
            //
            grpSearch.Controls.Add(txtKeyword);
            grpSearch.Controls.Add(btnSearch);
            grpSearch.Controls.Add(btnLoad);
            grpSearch.Location = new Point(23, 20);
            grpSearch.Margin = new Padding(3, 4, 3, 4);
            grpSearch.Name = "grpSearch";
            grpSearch.Padding = new Padding(3, 4, 3, 4);
            grpSearch.Size = new Size(974, 80);
            grpSearch.TabIndex = 0;
            grpSearch.TabStop = false;
            grpSearch.Text = "Tìm kiếm khách hàng";
            //
            // grpCustomerList
            //
            grpCustomerList.Controls.Add(dgvCustomers);
            grpCustomerList.Location = new Point(23, 113);
            grpCustomerList.Margin = new Padding(3, 4, 3, 4);
            grpCustomerList.Name = "grpCustomerList";
            grpCustomerList.Padding = new Padding(3, 4, 3, 4);
            grpCustomerList.Size = new Size(669, 460);
            grpCustomerList.TabIndex = 4;
            grpCustomerList.TabStop = false;
            grpCustomerList.Text = "Danh sách Khách hàng";
            //
            // grpCustomerInfo
            //
            grpCustomerInfo.Controls.Add(lblCustomerId);
            grpCustomerInfo.Controls.Add(txtCustomerId);
            grpCustomerInfo.Controls.Add(lblCustomerName);
            grpCustomerInfo.Controls.Add(txtCustomerName);
            grpCustomerInfo.Controls.Add(lblPhoneNumber);
            grpCustomerInfo.Controls.Add(txtPhoneNumber);
            grpCustomerInfo.Controls.Add(lblAddress);
            grpCustomerInfo.Controls.Add(txtAddress);
            grpCustomerInfo.Controls.Add(lblRewardPoints);
            grpCustomerInfo.Controls.Add(txtRewardPoints);
            grpCustomerInfo.Controls.Add(lblMembershipRank);
            grpCustomerInfo.Controls.Add(txtMembershipRank);
            grpCustomerInfo.Controls.Add(btnAdd);
            grpCustomerInfo.Controls.Add(btnUpdate);
            grpCustomerInfo.Controls.Add(btnDelete);
            grpCustomerInfo.Location = new Point(711, 113);
            grpCustomerInfo.Margin = new Padding(3, 4, 3, 4);
            grpCustomerInfo.Name = "grpCustomerInfo";
            grpCustomerInfo.Padding = new Padding(3, 4, 3, 4);
            grpCustomerInfo.Size = new Size(286, 460);
            grpCustomerInfo.TabIndex = 5;
            grpCustomerInfo.TabStop = false;
            grpCustomerInfo.Text = "Thông tin Khách hàng";
            //
            // lblCustomerId
            //
            lblCustomerId.AutoSize = true;
            lblCustomerId.Location = new Point(17, 32);
            lblCustomerId.Name = "lblCustomerId";
            lblCustomerId.Size = new Size(53, 20);
            lblCustomerId.TabIndex = 0;
            lblCustomerId.Text = "Mã KH";
            //
            // txtCustomerId
            //
            txtCustomerId.Location = new Point(17, 56);
            txtCustomerId.Margin = new Padding(3, 4, 3, 4);
            txtCustomerId.Name = "txtCustomerId";
            txtCustomerId.ReadOnly = true;
            txtCustomerId.Size = new Size(245, 27);
            txtCustomerId.TabIndex = 1;
            //
            // lblCustomerName
            //
            lblCustomerName.AutoSize = true;
            lblCustomerName.Location = new Point(17, 91);
            lblCustomerName.Name = "lblCustomerName";
            lblCustomerName.Size = new Size(115, 20);
            lblCustomerName.TabIndex = 2;
            lblCustomerName.Text = "Tên khách hàng";
            //
            // txtCustomerName
            //
            txtCustomerName.Location = new Point(17, 115);
            txtCustomerName.Margin = new Padding(3, 4, 3, 4);
            txtCustomerName.Name = "txtCustomerName";
            txtCustomerName.Size = new Size(245, 27);
            txtCustomerName.TabIndex = 3;
            //
            // lblPhoneNumber
            //
            lblPhoneNumber.AutoSize = true;
            lblPhoneNumber.Location = new Point(17, 150);
            lblPhoneNumber.Name = "lblPhoneNumber";
            lblPhoneNumber.Size = new Size(97, 20);
            lblPhoneNumber.TabIndex = 4;
            lblPhoneNumber.Text = "Số điện thoại";
            //
            // txtPhoneNumber
            //
            txtPhoneNumber.Location = new Point(17, 174);
            txtPhoneNumber.Margin = new Padding(3, 4, 3, 4);
            txtPhoneNumber.Name = "txtPhoneNumber";
            txtPhoneNumber.Size = new Size(245, 27);
            txtPhoneNumber.TabIndex = 5;
            //
            // lblAddress
            //
            lblAddress.AutoSize = true;
            lblAddress.Location = new Point(17, 209);
            lblAddress.Name = "lblAddress";
            lblAddress.Size = new Size(55, 20);
            lblAddress.TabIndex = 6;
            lblAddress.Text = "Địa chỉ";
            //
            // txtAddress
            //
            txtAddress.Location = new Point(17, 233);
            txtAddress.Margin = new Padding(3, 4, 3, 4);
            txtAddress.Name = "txtAddress";
            txtAddress.MaxLength = 200;
            txtAddress.PlaceholderText = "Số nhà, tên đường, phường/xã, tỉnh/thành phố";
            txtAddress.Multiline = true;
            txtAddress.WordWrap = true;
            txtAddress.Size = new Size(245, 75);
            txtAddress.TabIndex = 7;
            //
            // lblRewardPoints
            //
            lblRewardPoints.AutoSize = true;
            lblRewardPoints.Location = new Point(17, 316);
            lblRewardPoints.Name = "lblRewardPoints";
            lblRewardPoints.Size = new Size(95, 20);
            lblRewardPoints.TabIndex = 8;
            lblRewardPoints.Text = "Điểm tích lũy";
            //
            // txtRewardPoints
            //
            txtRewardPoints.Location = new Point(17, 340);
            txtRewardPoints.Margin = new Padding(3, 4, 3, 4);
            txtRewardPoints.Name = "txtRewardPoints";
            txtRewardPoints.Size = new Size(100, 27);
            txtRewardPoints.TabIndex = 9;
            txtRewardPoints.Text = "0";
            //
            // lblMembershipRank
            //
            lblMembershipRank.AutoSize = true;
            lblMembershipRank.Location = new Point(139, 316);
            lblMembershipRank.Name = "lblMembershipRank";
            lblMembershipRank.Size = new Size(67, 20);
            lblMembershipRank.TabIndex = 10;
            lblMembershipRank.Text = "Hạng thẻ";
            //
            // txtMembershipRank
            //
            txtMembershipRank.Location = new Point(139, 340);
            txtMembershipRank.Margin = new Padding(3, 4, 3, 4);
            txtMembershipRank.Name = "txtMembershipRank";
            txtMembershipRank.Size = new Size(123, 27);
            txtMembershipRank.TabIndex = 11;
            txtMembershipRank.Text = "Chuẩn";
            //
            // btnAdd
            //
            btnAdd.Location = new Point(17, 408);
            btnAdd.Margin = new Padding(3, 4, 3, 4);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(74, 40);
            btnAdd.TabIndex = 12;
            btnAdd.Text = "Thêm";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            //
            // btnUpdate
            //
            btnUpdate.Location = new Point(97, 408);
            btnUpdate.Margin = new Padding(3, 4, 3, 4);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(86, 40);
            btnUpdate.TabIndex = 13;
            btnUpdate.Text = "Cập nhật";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += btnUpdate_Click;
            //
            // btnDelete
            //
            btnDelete.Location = new Point(189, 408);
            btnDelete.Margin = new Padding(3, 4, 3, 4);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(73, 40);
            btnDelete.TabIndex = 14;
            btnDelete.Text = "Xóa";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            //
            // FormCustomerManagement
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(1020, 600);
            var layout = new TableLayoutPanel
            {
                Name = "customerLayout", Dock = DockStyle.Fill,
                ColumnCount = 2, RowCount = 2, Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 286));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grpSearch.Dock = DockStyle.Fill;
            grpCustomerList.Dock = DockStyle.Fill;
            grpCustomerInfo.Dock = DockStyle.Fill;
            layout.Controls.Add(grpSearch, 0, 0);
            layout.SetColumnSpan(grpSearch, 2);
            layout.Controls.Add(grpCustomerList, 0, 1);
            layout.Controls.Add(grpCustomerInfo, 1, 1);
            Controls.Add(layout);
            Margin = new Padding(3, 4, 3, 4);
            Name = "FormCustomerManagement";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Quản lý Khách hàng - Tạp hóa Tiện lợi Minh Khang";
            Load += FormCustomerManagement_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
            grpSearch.ResumeLayout(false);
            grpSearch.PerformLayout();
            grpCustomerList.ResumeLayout(false);
            grpCustomerInfo.ResumeLayout(false);
            grpCustomerInfo.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgvCustomers;
        private TextBox txtKeyword;
        private Button btnSearch;
        private Button btnLoad;
        private GroupBox grpSearch;
        private GroupBox grpCustomerList;
        private GroupBox grpCustomerInfo;
        private Label lblCustomerId;
        private TextBox txtCustomerId;
        private Label lblCustomerName;
        private TextBox txtCustomerName;
        private Label lblPhoneNumber;
        private TextBox txtPhoneNumber;
        private Label lblAddress;
        private TextBox txtAddress;
        private Label lblRewardPoints;
        private TextBox txtRewardPoints;
        private Label lblMembershipRank;
        private TextBox txtMembershipRank;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
    }
}
