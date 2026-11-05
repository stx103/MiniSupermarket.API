namespace MiniSupermarket.API.Models
{
    // [Tiêu chí 15] DTO trả dữ liệu qua API: chọn các trường cần thiết, không trả nguyên entity hay mật khẩu tài khoản.
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CustomerDto
    {
        public DateTime? CreatedAt { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int RewardPoints { get; set; }
        public string MembershipRank { get; set; } = string.Empty;
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

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class AccessInfoDto
    {
        public string Message { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RevenueReportDto
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; }
        public int TotalCashiers { get; set; }
        public decimal GrandTotalRevenue { get; set; }
        public List<CashierRevenueDto> Data { get; set; } = new();
    }

    public class QuickReportDto
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public string BestSeller { get; set; } = string.Empty;
        public long BestSellerQuantity { get; set; }
    }

    public class OrderDto
    {
        public int RedeemedPoints { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int RewardPoints { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CashierUsername { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CancelOrderResult
    {
        public bool Success { get; set; } = true;
        public int OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ProductCountDto
    {
        public int Total { get; set; }
    }

    public class CategoryCountDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class ProductCacheDto
    {
        public int TotalLoaded { get; set; }
        public DateTime CachedAt { get; set; }
        public List<ProductItemDto> Data { get; set; } = new();
    }
}
