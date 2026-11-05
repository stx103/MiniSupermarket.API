using System.ComponentModel.DataAnnotations;

namespace MiniSupermarket.API.Models
{
    // [Tiêu chí 15] DTO nhận dữ liệu từ client: các lớp Request chỉ chứa trường được phép nhập, tách khỏi entity lưu CSDL.
    public class LoginRequestDto
    {
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class CategoryRequest
    {
        [Required, StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;
        [StringLength(255)]
        public string? Description { get; set; }
    }

    public class CustomerRequest
    {
        [Required, StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        [Required, StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ khách hàng.")]
        [StringLength(200)]
        [RegularExpression(CustomerAddressFormat.Pattern, ErrorMessage = CustomerAddressFormat.ErrorMessage)]
        public string Address { get; set; } = string.Empty;
        [Range(0, int.MaxValue)]
        public int RewardPoints { get; set; }
        [StringLength(50)]
        public string MembershipRank { get; set; } = "Chuẩn";
    }

    // [Tiêu chí 20] Validation trên DTO: Required bắt buộc nhập, StringLength giới hạn độ dài, Range yêu cầu giá > 0 và tồn kho >= 0.
    public class ProductRequest
    {
        [Required, StringLength(50)]
        public string Barcode { get; set; } = string.Empty;
        [Required, StringLength(150)]
        public string ProductName { get; set; } = string.Empty;
        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Price { get; set; }
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }

    public class CreateUserRequest
    {
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        [EmailAddress, StringLength(254)]
        public string? Email { get; set; }
        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        [Required, RegularExpression("^(Admin|Cashier|Warehouse)$")]
        public string Role { get; set; } = "Cashier";
    }

    public class ResetPasswordRequest
    {
        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }

    public class SetUserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class CheckoutRequest
    {
        [Range(0, int.MaxValue)]
        public int PointsToRedeem { get; set; }
        [Range(1, int.MaxValue)]
        public int? CustomerId { get; set; }
        [StringLength(50)]
        public string? CashierUsername { get; set; }
        [StringLength(15)]
        public string? CustomerPhone { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? CashReceived { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? ExpectedTotalAmount { get; set; }
        [Required, MinLength(1), MaxLength(200)]
        public List<CheckoutItemRequest> Items { get; set; } = new();
    }

    public class CheckoutItemRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }
        [Range(1, 1000000)]
        public int Quantity { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? UnitPrice { get; set; }
    }

    public class StockUpdateRequest
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }
    }
}
