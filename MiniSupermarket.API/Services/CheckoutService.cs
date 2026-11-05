using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using MiniSupermarket.API.Data;
using MiniSupermarket.API.Models;
using static MiniSupermarket.API.Data.AdoDatabase;

namespace MiniSupermarket.API.Services
{
    public sealed class PosException : Exception
    {
        public int StatusCode { get; }
        public PosException(int statusCode, string message) : base(message) => StatusCode = statusCode;
    }

    public sealed class CheckoutService
    {
        private readonly AdoDatabase _database;
        private readonly ILogger<CheckoutService> _logger;
        public CheckoutService(AdoDatabase database, ILogger<CheckoutService> logger) { _database = database; _logger = logger; }

        // [Tiêu chí 35] Thanh toán nguyên tử: lưu hóa đơn/chi tiết, trừ tồn kho và cộng điểm trong một SqlTransaction trên SQL Server.
        public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, string username, CancellationToken cancellationToken)
        {
            if (request.PointsToRedeem < 0 || request.Items.Count == 0 || request.Items.Any(i => i.Quantity <= 0)
                || request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new PosException(400, "Giỏ hàng rỗng, số lượng không hợp lệ hoặc trùng sản phẩm.");
            if (!string.IsNullOrWhiteSpace(request.CashierUsername)
                && !string.Equals(request.CashierUsername.Trim(), username, StringComparison.OrdinalIgnoreCase))
                throw new PosException(403, "Không được lập hóa đơn dưới tài khoản người khác.");

            await using var lease = await _database.OpenAsync(cancellationToken);
            var connection = lease.Connection;
            // [Tiêu chí 35] Bắt đầu giao dịch; mọi Command trong khối try đều nhận cùng biến transaction này.
            await using var transaction = await BeginTransactionAsync(connection, cancellationToken);
            try
            {
                using var cashierCommand = Command(connection, "SELECT UserId FROM Users WHERE Username = @Username AND IsActive = 1 AND [Role] IN ('Admin', 'Cashier')", transaction);
                Parameter(cashierCommand, "@Username", username, DbType.String, 50);
                var cashierId = await cashierCommand.ExecuteScalarAsync(cancellationToken);
                if (cashierId == null) throw new PosException(401, "Tài khoản không còn quyền bán hàng.");

                int? customerId = null;
                if (request.CustomerId.HasValue || !string.IsNullOrWhiteSpace(request.CustomerPhone))
                {
                    var byId = request.CustomerId.HasValue;
                    using var customerCommand = Command(connection,
                        byId ? "SELECT CustomerId, PhoneNumber FROM Customers WHERE CustomerId = @Id"
                             : "SELECT CustomerId, PhoneNumber FROM Customers WHERE PhoneNumber = @Phone", transaction);
                    if (byId) Parameter(customerCommand, "@Id", request.CustomerId, DbType.Int32);
                    else Parameter(customerCommand, "@Phone", request.CustomerPhone!.Trim(), DbType.AnsiString, 15);
                    using var reader = await customerCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken)) throw new PosException(400, "Khách hàng chưa đăng ký. Hãy thêm thành viên hoặc để trống.");
                    customerId = reader.GetInt32(0);
                    if (byId && !string.IsNullOrWhiteSpace(request.CustomerPhone)
                        && reader.GetString(1) != request.CustomerPhone.Trim())
                        throw new PosException(400, "Mã khách hàng và số điện thoại không khớp.");
                }

                var details = new List<OrderDetail>();
                foreach (var item in request.Items.OrderBy(i => i.ProductId))
                {
                    using var productCommand = Command(connection, "SELECT Price FROM Products WHERE ProductId = @Id", transaction);
                    Parameter(productCommand, "@Id", item.ProductId, DbType.Int32);
                    var value = await productCommand.ExecuteScalarAsync(cancellationToken);
                    if (value == null) throw new PosException(400, $"Sản phẩm {item.ProductId} không tồn tại.");
                    var price = Convert.ToDecimal(value);
                    if (item.UnitPrice.HasValue && item.UnitPrice.Value != price)
                        throw new PosException(409, "Giá bán đã thay đổi. Vui lòng quét lại sản phẩm.");
                    details.Add(new OrderDetail { ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = price, LineTotal = price * item.Quantity });
                }

                var total = details.Sum(d => d.LineTotal);
                // 1 điểm = 1.000đ; API tính lại mức giảm, không nhận FinalAmount từ client.
                var discount = request.PointsToRedeem * 1000m;
                if (request.PointsToRedeem > 0 && !customerId.HasValue)
                    throw new PosException(400, "Phải chọn khách thành viên để đổi điểm.");
                if (discount > total) throw new PosException(400, "Giá trị đổi điểm vượt tổng tiền hàng.");
                var final = total - discount;
                if (request.ExpectedTotalAmount.HasValue && total != request.ExpectedTotalAmount.Value)
                    throw new PosException(409, "Giá bán đã thay đổi. Vui lòng cập nhật giỏ hàng.");
                if (total > 9999999999999999.99m || request.CashReceived.HasValue && request.CashReceived.Value < final)
                    throw new PosException(400, "Tiền khách đưa chưa đủ hoặc tổng hóa đơn vượt giới hạn.");
                if (customerId.HasValue && final / 10000m > int.MaxValue)
                    throw new PosException(400, "Điểm thưởng vượt giới hạn cho phép.");

                var result = new CheckoutResult
                {
                    OrderCode = $"HD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..16].ToUpperInvariant()}",
                    OrderDate = DateTime.Now, TotalAmount = total, TotalItems = details.Sum(d => d.Quantity),
                    RedeemedPoints = request.PointsToRedeem, DiscountAmount = discount,
                    RewardPointsAdded = customerId.HasValue ? (int)(final / 10000m) : 0,
                    Change = (request.CashReceived ?? final) - final
                };
                // [Tiêu chí 35] Lưu đầu hóa đơn trong giao dịch; nếu bước sau lỗi, dòng Orders này cũng bị Rollback.
                var insert = connection is SqlConnection
                    ? "INSERT INTO Orders (OrderCode, OrderDate, CustomerId, UserId, TotalAmount, RewardPoints, RedeemedPoints, DiscountAmount, Status) OUTPUT INSERTED.OrderId VALUES (@Code, @Date, @Customer, @User, @Total, @Points, @Redeemed, @Discount, @Status)"
                    : "INSERT INTO Orders (OrderCode, OrderDate, CustomerId, UserId, TotalAmount, RewardPoints, RedeemedPoints, DiscountAmount, Status) VALUES (@Code, @Date, @Customer, @User, @Total, @Points, @Redeemed, @Discount, @Status) RETURNING OrderId";
                using (var command = Command(connection, insert, transaction))
                {
                    Parameter(command, "@Code", result.OrderCode, DbType.AnsiString, 30);
                    Parameter(command, "@Date", result.OrderDate, DbType.DateTime2);
                    Parameter(command, "@Customer", customerId, DbType.Int32);
                    Parameter(command, "@User", Convert.ToInt32(cashierId), DbType.Int32);
                    Parameter(command, "@Total", total, DbType.Decimal);
                    Parameter(command, "@Points", result.RewardPointsAdded, DbType.Int32);
                    Parameter(command, "@Redeemed", request.PointsToRedeem, DbType.Int32);
                    Parameter(command, "@Discount", discount, DbType.Decimal);
                    Parameter(command, "@Status", "Hoàn tất", DbType.String, 30);
                    result.OrderId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
                }

                // Every command shares this transaction; even a failure on the last item undoes earlier rows.
                foreach (var detail in details)
                {
                    // [Tiêu chí 35] Chỉ trừ kho khi đủ hàng và đúng giá; cập nhật thất bại sẽ ném lỗi để catch hoàn tác toàn bộ hóa đơn.
                    using var stock = Command(connection, "UPDATE Products SET StockQuantity = StockQuantity - @Qty WHERE ProductId = @Id AND StockQuantity >= @Qty AND Price = @Price", transaction);
                    Parameter(stock, "@Qty", detail.Quantity, DbType.Int32);
                    Parameter(stock, "@Id", detail.ProductId, DbType.Int32);
                    Parameter(stock, "@Price", detail.UnitPrice, DbType.Decimal);
                    if (await stock.ExecuteNonQueryAsync(cancellationToken) != 1)
                        throw new PosException(409, $"Sản phẩm {detail.ProductId} không đủ tồn kho hoặc giá vừa thay đổi. Giao dịch đã được hoàn tác.");
                    using var line = Command(connection, "INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice, LineTotal) VALUES (@Order, @Product, @Qty, @Price, @Total)", transaction);
                    Parameter(line, "@Order", result.OrderId, DbType.Int32);
                    Parameter(line, "@Product", detail.ProductId, DbType.Int32);
                    Parameter(line, "@Qty", detail.Quantity, DbType.Int32);
                    Parameter(line, "@Price", detail.UnitPrice, DbType.Decimal);
                    Parameter(line, "@Total", detail.LineTotal, DbType.Decimal);
                    await line.ExecuteNonQueryAsync(cancellationToken);
                }

                // [Tiêu chí 35] Cộng điểm khách thành viên bằng cùng transaction với hóa đơn và tồn kho.
                if (customerId.HasValue)
                {
                    using var points = Command(connection, "UPDATE Customers SET RewardPoints = RewardPoints - @Redeemed + @Points WHERE CustomerId = @Id AND RewardPoints >= @Redeemed AND CAST(RewardPoints AS bigint) - @Redeemed + @Points <= @Limit", transaction);
                    Parameter(points, "@Points", result.RewardPointsAdded, DbType.Int32);
                    Parameter(points, "@Id", customerId, DbType.Int32);
                    Parameter(points, "@Redeemed", request.PointsToRedeem, DbType.Int32);
                    Parameter(points, "@Limit", int.MaxValue, DbType.Int32);
                    if (await points.ExecuteNonQueryAsync(cancellationToken) != 1)
                        throw new PosException(409, "Không đủ điểm để đổi hoặc điểm vượt giới hạn. Giao dịch đã được hoàn tác.");
                }
                // [Tiêu chí 35] Chỉ Commit khi tất cả bước đều thành công; lúc này mới xác nhận thanh toán.
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                // [Tiêu chí 35] Thiếu hàng hoặc bất kỳ bước nào lỗi: Rollback cả hóa đơn, chi tiết, kho và điểm; không lưu hóa đơn rác.
                await RollbackAsync(transaction);
                throw;
            }
        }

        public async Task<CancelOrderResult> CancelAsync(int orderId, string username, bool isAdmin, CancellationToken cancellationToken)
        {
            await using var lease = await _database.OpenAsync(cancellationToken);
            var connection = lease.Connection;
            await using var transaction = await BeginTransactionAsync(connection, cancellationToken);
            try
            {
                int? customerId;
                int rewardPoints, redeemedPoints;
                using (var command = Command(connection, "SELECT o.CustomerId, o.RewardPoints, u.Username, o.RedeemedPoints FROM Orders o INNER JOIN Users u ON u.UserId = o.UserId WHERE o.OrderId = @Id", transaction))
                {
                    Parameter(command, "@Id", orderId, DbType.Int32);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken)) throw new PosException(404, "Không tìm thấy hóa đơn cần hủy.");
                    if (!isAdmin && !string.Equals(reader.GetString(2), username, StringComparison.OrdinalIgnoreCase))
                        throw new PosException(403, "Thu ngân chỉ được hủy hóa đơn do mình lập.");
                    customerId = reader.IsDBNull(0) ? null : reader.GetInt32(0);
                    rewardPoints = reader.GetInt32(1); redeemedPoints = reader.GetInt32(3);
                }

                using (var claim = Command(connection, "UPDATE Orders SET Status = @Cancelling WHERE OrderId = @Id AND Status = @Completed", transaction))
                {
                    Parameter(claim, "@Id", orderId, DbType.Int32);
                    Parameter(claim, "@Cancelling", "Đang hủy", DbType.String, 30);
                    Parameter(claim, "@Completed", "Hoàn tất", DbType.String, 30);
                    if (await claim.ExecuteNonQueryAsync(cancellationToken) != 1) throw new PosException(409, "Hóa đơn đã hủy hoặc đang được xử lý.");
                }

                var items = new List<(int Id, int Quantity)>();
                using (var command = Command(connection, "SELECT ProductId, Quantity FROM OrderDetails WHERE OrderId = @Id ORDER BY ProductId", transaction))
                {
                    Parameter(command, "@Id", orderId, DbType.Int32);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken)) items.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
                foreach (var item in items)
                {
                    using var stock = Command(connection, "UPDATE Products SET StockQuantity = StockQuantity + @Qty WHERE ProductId = @Id AND StockQuantity <= @Limit", transaction);
                    Parameter(stock, "@Qty", item.Quantity, DbType.Int32);
                    Parameter(stock, "@Id", item.Id, DbType.Int32);
                    Parameter(stock, "@Limit", int.MaxValue - item.Quantity, DbType.Int32);
                    if (await stock.ExecuteNonQueryAsync(cancellationToken) != 1) throw new PosException(409, "Không thể hoàn kho; toàn bộ thao tác hủy được hoàn tác.");
                }
                if (customerId.HasValue && (rewardPoints > 0 || redeemedPoints > 0))
                {
                    using var points = Command(connection, "UPDATE Customers SET RewardPoints = RewardPoints - @Points + @Redeemed WHERE CustomerId = @Id AND CAST(RewardPoints AS bigint) - @Points + @Redeemed BETWEEN 0 AND 2147483647", transaction);
                    Parameter(points, "@Id", customerId, DbType.Int32);
                    Parameter(points, "@Points", rewardPoints, DbType.Int32);
                    Parameter(points, "@Redeemed", redeemedPoints, DbType.Int32);
                    if (await points.ExecuteNonQueryAsync(cancellationToken) != 1) throw new PosException(409, "Khách không còn đủ điểm để hoàn trả; chưa hủy hóa đơn.");
                }
                foreach (var sql in new[] { "DELETE FROM OrderDetails WHERE OrderId = @Id", "DELETE FROM Orders WHERE OrderId = @Id" })
                {
                    using var command = Command(connection, sql, transaction);
                    Parameter(command, "@Id", orderId, DbType.Int32);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                return new CancelOrderResult { OrderId = orderId, Message = "Đã hủy hóa đơn, hoàn lại tồn kho và điểm thành viên." };
            }
            catch
            {
                await RollbackAsync(transaction);
                throw;
            }
        }

        // [Tiêu chí 35] RollbackAsync gọi transaction.RollbackAsync; CancellationToken.None vẫn cho phép hoàn tác khi yêu cầu HTTP bị hủy.
        private async Task RollbackAsync(DbTransaction transaction)
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Transaction rollback failed after checkout/cancellation error."); }
        }
    }
}
