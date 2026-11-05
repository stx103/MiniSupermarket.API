using System.Net.Http.Json;

namespace MiniSupermarket.WinForms
{
    public partial class FormPOS : Form
    {
        private readonly List<CartItemDto> _cart = new();
        private CustomerDto? _customer;
        public bool HasCart => _cart.Count > 0;
        public bool IsBusy { get; private set; }
        public FormPOS() => InitializeComponent();

        private async void txtBarcode_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await AddBarcodeAsync();
        }

        private async Task AddBarcodeAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(txtBarcode.Text)) return;
            SetBusy(true);
            try
            {
                var product = await ApiClientService.GetAsync<ProductDto>($"ado-products/barcode/{Uri.EscapeDataString(txtBarcode.Text.Trim())}");
                var item = _cart.FirstOrDefault(i => i.ProductId == product.ProductId);
                int quantity = (item?.Quantity ?? 0) + 1;
                if (quantity > product.StockQuantity) throw new InvalidOperationException($"409 - Sản phẩm chỉ còn {product.StockQuantity} trong kho.");
                if (item == null) _cart.Add(new CartItemDto { ProductId = product.ProductId, Barcode = product.Barcode, ProductName = product.ProductName, UnitPrice = product.Price, Quantity = 1 });
                else { item.Quantity = quantity; item.UnitPrice = product.Price; }
                txtBarcode.Clear();
                UpdateCartDisplay();
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
            finally { SetBusy(false); txtBarcode.Focus(); }
        }

        private void SetBusy(bool value) { IsBusy = value; Enabled = !value; }

        private void UpdateCartDisplay()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = _cart.ToList();
            lblTotalAmount.Text = _cart.Sum(i => i.TotalPrice).ToString("N0") + " đ";
            CalculateChange();
        }

        private void CalculateChange()
        {
            decimal total = _cart.Sum(i => i.TotalPrice);
            int maxPoints = _customer == null ? 0 : (int)Math.Min(_customer.RewardPoints, Math.Floor(total / 1000m));
            nudRedeemPoints.Maximum = maxPoints;
            decimal discount = nudRedeemPoints.Value * 1000m;
            total -= discount;
            lblDiscount.Text = $"Giảm: {discount:N0} đ ({nudRedeemPoints.Value:N0} điểm)";
            lblFinalAmount.Text = total.ToString("N0") + " đ";
            bool valid = decimal.TryParse(txtCashReceived.Text, out decimal cash) && cash >= 0;
            lblChange.Text = !HasCart ? "0 đ" : valid && cash >= total ? (cash - total).ToString("N0") + " đ" : "Chưa đủ tiền";
            lblChange.ForeColor = !HasCart || valid && cash >= total ? Color.Black : Color.Firebrick;
        }

        private async Task FindCustomerAsync()
        {
            if (IsBusy) return;
            lblCustomerName.Text = "Khách vãng lai";
            ResetCustomer();
            if (string.IsNullOrWhiteSpace(txtCustomerPhone.Text)) return;
            SetBusy(true);
            try
            {
                var customer = await ApiClientService.GetAsync<CustomerDto>($"customers/phone/{Uri.EscapeDataString(txtCustomerPhone.Text.Trim())}");
                _customer = customer;
                lblCustomerName.Text = $"{customer.CustomerName}\n{customer.MembershipRank} | {customer.RewardPoints:N0} điểm";
                CalculateChange();
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
            finally { SetBusy(false); }
        }

        private async void btnCheckout_Click(object? sender, EventArgs e)
        {
            if (IsBusy) return;
            if (!SessionManager.CanAccess("POS")) { MessageBox.Show("403 - Bạn không có quyền bán hàng."); return; }
            if (!HasCart) { WorkspaceUi.Invalid("Giỏ hàng đang trống."); return; }
            var total = _cart.Sum(i => i.TotalPrice);
            var points = (int)nudRedeemPoints.Value;
            if (!decimal.TryParse(txtCashReceived.Text, out decimal cash) || cash < total - points * 1000m)
            { WorkspaceUi.Invalid("Tiền khách đưa chưa đủ hoặc không hợp lệ."); return; }
            SetBusy(true);
            try
            {
                using var response = await ApiClientService.Client.PostAsJsonAsync("checkout", new
                {
                    CustomerPhone = txtCustomerPhone.Text.Trim(), CashReceived = cash, ExpectedTotalAmount = total, PointsToRedeem = points,
                    Items = _cart.Select(i => new { i.ProductId, i.Quantity }).ToList()
                });
                await ApiClientService.EnsureSuccessAsync(response);
                var result = await response.Content.ReadFromJsonAsync<CheckoutResultDto>();
                nudCancelOrderId.Value = result!.OrderId;
                _cart.Clear();
                txtCashReceived.Clear(); txtCustomerPhone.Clear();
                UpdateCartDisplay();
                WorkspaceUi.Success(response, $"Đã lưu hóa đơn {result.OrderCode} (#{result.OrderId})\nTiền hàng: {result.TotalAmount:N0} đ\nĐổi {result.RedeemedPoints:N0} điểm, giảm {result.DiscountAmount:N0} đ\nThanh toán: {result.FinalAmount:N0} đ\nTrả lại: {result.Change:N0} đ\nĐiểm tích thêm: {result.RewardPointsAdded:N0}");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
            finally { SetBusy(false); txtBarcode.Focus(); }
        }

        private void btnClearCart_Click(object? sender, EventArgs e)
        {
            if (IsBusy || !HasCart) return;
            if (MessageBox.Show("Hủy toàn bộ giỏ hàng?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            _cart.Clear(); txtCashReceived.Clear(); txtCustomerPhone.Clear();
            UpdateCartDisplay();
        }

        private async void btnSetQuantity_Click(object? sender, EventArgs e)
        {
            if (IsBusy || dgvCart.CurrentRow?.DataBoundItem is not CartItemDto selected) return;
            SetBusy(true);
            try
            {
                var source = _cart.First(i => i.ProductId == selected.ProductId);
                var product = await ApiClientService.GetAsync<ProductDto>($"ado-products/barcode/{Uri.EscapeDataString(source.Barcode)}");
                if (nudQuantity.Value > product.StockQuantity) throw new InvalidOperationException($"409 - Chỉ còn {product.StockQuantity} sản phẩm trong kho.");
                source.Quantity = (int)nudQuantity.Value;
                source.UnitPrice = product.Price;
                UpdateCartDisplay();
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
            finally { SetBusy(false); }
        }

        private void btnRemoveItem_Click(object? sender, EventArgs e)
        {
            if (IsBusy || dgvCart.CurrentRow?.DataBoundItem is not CartItemDto item) return;
            _cart.RemoveAll(i => i.ProductId == item.ProductId);
            UpdateCartDisplay();
        }

        private async void btnCancelOrder_Click(object? sender, EventArgs e)
        {
            if (IsBusy || !SessionManager.CanAccess("POS")) return;
            if (HasCart) { WorkspaceUi.Invalid("Hoàn tất hoặc hủy giỏ hàng hiện tại trước khi hủy hóa đơn."); return; }
            int id = (int)nudCancelOrderId.Value;
            if (id <= 0) { WorkspaceUi.Invalid("Nhập mã hóa đơn cần hủy."); return; }
            if (MessageBox.Show($"Hủy hóa đơn #{id}, hoàn tồn kho, hoàn điểm đã đổi và thu hồi điểm đã cộng?", "Xác nhận hủy hóa đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            SetBusy(true);
            try
            {
                using var response = await ApiClientService.Client.DeleteAsync($"checkout/cancel/{id}");
                await ApiClientService.EnsureSuccessAsync(response);
                nudCancelOrderId.Value = 0;
                WorkspaceUi.Success(response, "Đã hủy hóa đơn, hoàn lại tồn kho và điểm thành viên.");
            }
            catch (Exception ex) { WorkspaceUi.Error(ex); }
            finally { SetBusy(false); }
        }

        private void ResetCustomer()
        {
            _customer = null;
            nudRedeemPoints.Value = 0;
            nudRedeemPoints.Maximum = 0;
            CalculateChange();
        }
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CheckoutResultDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int RedeemedPoints { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal Change { get; set; }
        public int RewardPointsAdded { get; set; }
    }
}
