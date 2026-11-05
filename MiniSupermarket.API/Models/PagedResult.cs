using Microsoft.EntityFrameworkCore;

namespace MiniSupermarket.API.Models
{
    public class PagedResult<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; } = new();

        // [Tiêu chí 25] Đếm tổng số dòng và giới hạn trang trên IQueryable; dữ liệu chỉ được tải về sau Skip/Take tại CSDL.
        public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> orderedQuery, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageSize = pageSize is < 1 or > 50 ? 5 : pageSize;
            var total = await orderedQuery.CountAsync(cancellationToken);
            var pages = (int)Math.Ceiling((double)total / pageSize);
            var current = Math.Clamp(pageNumber, 1, Math.Max(1, pages));
            return new PagedResult<T>
            {
                CurrentPage = current, PageSize = pageSize, TotalItems = total, TotalPages = pages,
                // [Tiêu chí 25] Skip bỏ qua dòng của các trang trước, Take lấy tối đa PageSize dòng cho trang hiện tại.
                Items = await orderedQuery.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken)
            };
        }
    }

    public class ProductItemDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CashierRevenueDto
    {
        public string CashierUsername { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public long TotalPointsAwarded { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public long TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CheckoutResult
    {
        public bool Success => true;
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public int RedeemedPoints { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount => TotalAmount - DiscountAmount;
        public decimal Change { get; set; }
        public int RewardPointsAdded { get; set; }
    }
}
