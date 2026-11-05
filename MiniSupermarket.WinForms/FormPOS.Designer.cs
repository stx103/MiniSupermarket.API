namespace MiniSupermarket.WinForms
{
    partial class FormPOS
    {
        private TextBox txtBarcode = null!, txtCustomerPhone = null!, txtCashReceived = null!;
        private DataGridView dgvCart = null!;
        private Label lblCustomerName = null!, lblTotalAmount = null!, lblChange = null!;
        private NumericUpDown nudQuantity = null!;
        private NumericUpDown nudCancelOrderId = null!;
        private NumericUpDown nudRedeemPoints = null!;
        private Label lblDiscount = null!, lblFinalAmount = null!;

        private void InitializeComponent()
        {
            WorkspaceUi.Configure(this);
            KeyPreview = true;
            txtBarcode = new TextBox { Name = "txtBarcode", Width = 300, MaxLength = 50, PlaceholderText = "Mã vạch sản phẩm" };
            txtBarcode.KeyDown += txtBarcode_KeyDown;
            var toolbar = WorkspaceUi.Toolbar();
            toolbar.Controls.AddRange(new Control[] { txtBarcode,
                WorkspaceUi.Button("btnAddBarcode", "Thêm vào giỏ", async (_, _) => await AddBarcodeAsync()) });
            dgvCart = WorkspaceUi.Grid("dgvCart");
            WorkspaceUi.Column(dgvCart, "ProductId", "Mã SP", 50);
            WorkspaceUi.Column(dgvCart, "ProductName", "Tên sản phẩm", 200);
            WorkspaceUi.Column(dgvCart, "UnitPrice", "Đơn giá", 90, "N0");
            WorkspaceUi.Column(dgvCart, "Quantity", "SL", 50);
            WorkspaceUi.Column(dgvCart, "TotalPrice", "Thành tiền", 100, "N0");
            dgvCart.CellClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvCart.Rows[e.RowIndex].DataBoundItem is CartItemDto item) nudQuantity.Value = item.Quantity;
            };
            var editor = WorkspaceUi.Editor();
            txtCustomerPhone = new TextBox { Name = "txtCustomerPhone", MaxLength = 15 };
            txtCustomerPhone.TextChanged += (_, _) => { lblCustomerName.Text = "Khách vãng lai"; ResetCustomer(); };
            txtCustomerPhone.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await FindCustomerAsync(); } };
            lblCustomerName = new Label { Name = "lblCustomerName", Text = "Khách vãng lai", Width = 260, Height = 48 };
            lblTotalAmount = new Label { Name = "lblTotalAmount", Text = "0 đ", Width = 260, Height = 44, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            txtCashReceived = new TextBox { Name = "txtCashReceived", MaxLength = 18 };
            txtCashReceived.TextChanged += (_, _) => CalculateChange();
            lblChange = new Label { Name = "lblChange", Text = "0 đ", Width = 260, Height = 34, Font = new Font("Segoe UI", 13F, FontStyle.Bold) };
            nudRedeemPoints = new NumericUpDown { Name = "nudRedeemPoints", Maximum = 0, Increment = 50, ThousandsSeparator = true };
            lblDiscount = new Label { Name = "lblDiscount", Text = "Giảm: 0 đ", Width = 260, Height = 24, ForeColor = Color.Firebrick };
            lblFinalAmount = new Label { Name = "lblFinalAmount", Text = "0 đ", Width = 260, Height = 38, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            nudRedeemPoints.ValueChanged += (_, _) => CalculateChange();
            WorkspaceUi.Field(editor, "Số điện thoại thành viên", txtCustomerPhone);
            editor.Controls.Add(WorkspaceUi.Button("btnFindCustomer", "Tìm khách hàng", async (_, _) => await FindCustomerAsync()));
            editor.Controls.Add(lblCustomerName);
            WorkspaceUi.Field(editor, "Tiền hàng trước giảm", lblTotalAmount);
            WorkspaceUi.Field(editor, "Đổi điểm (1 điểm = 1.000đ)", nudRedeemPoints);
            editor.Controls.Add(lblDiscount);
            WorkspaceUi.Field(editor, "Khách cần thanh toán", lblFinalAmount);
            WorkspaceUi.Field(editor, "Tiền khách đưa (đ)", txtCashReceived);
            WorkspaceUi.Field(editor, "Tiền trả lại", lblChange);
            var checkout = WorkspaceUi.Button("btnCheckout", "Thanh toán", btnCheckout_Click);
            checkout.BackColor = Color.FromArgb(32, 112, 70); checkout.ForeColor = Color.White;
            editor.Controls.Add(checkout);
            var clear = WorkspaceUi.Button("btnClearCart", "Hủy giỏ hàng", btnClearCart_Click);
            clear.ForeColor = Color.Firebrick;
            editor.Controls.Add(clear);
            nudQuantity = new NumericUpDown { Name = "nudQuantity", Minimum = 1, Maximum = 1000000 };
            WorkspaceUi.Field(editor, "Số lượng dòng đang chọn", nudQuantity);
            var actions = new FlowLayoutPanel { AutoSize = true, Width = 260, MaximumSize = new Size(260, 0) };
            actions.Controls.AddRange(new Control[] {
                WorkspaceUi.Button("btnSetQuantity", "Đổi số lượng", btnSetQuantity_Click),
                WorkspaceUi.Button("btnRemoveItem", "Bỏ mặt hàng", btnRemoveItem_Click) });
            editor.Controls.Add(actions);
            nudCancelOrderId = new NumericUpDown { Name = "nudCancelOrderId", Maximum = int.MaxValue };
            WorkspaceUi.Field(editor, "Mã hóa đơn cần hủy", nudCancelOrderId);
            var cancelOrder = WorkspaceUi.Button("btnCancelOrder", "Hủy hóa đơn", btnCancelOrder_Click);
            cancelOrder.ForeColor = Color.Firebrick;
            editor.Controls.Add(cancelOrder);
            WorkspaceUi.Mount(this, toolbar, dgvCart, editor);
            KeyDown += (_, e) => { if (e.KeyCode == Keys.F9) { e.SuppressKeyPress = true; btnCheckout_Click(this, EventArgs.Empty); } };
        }
    }
}
