namespace MiniSupermarket.API.Models
{
    public class Category
    {
        // Mã định danh nhóm hàng (Khóa chính)
        public int CategoryId { get; set; }

        // Tên nhóm hàng (Bắt buộc, không được để trống)
        public string CategoryName { get; set; } = string.Empty;

        // Mô tả chi tiết về nhóm hàng (Có thể để trống)
        public string? Description { get; set; }
    }
}
