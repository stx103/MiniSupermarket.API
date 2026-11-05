using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniSupermarket.API.Models
{
    // [Tiêu chí 18] Entity Order ánh xạ bảng Orders: hóa đơn, ngày lập, khách hàng, nhân viên, tổng tiền và điểm thưởng.
    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [StringLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string OrderCode { get; set; } = string.Empty;

        public int RewardPoints { get; set; }
        // Đổi điểm: tiền gốc, giảm giá và tiền thực thu được lưu cùng giao dịch.
        public int RedeemedPoints { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(19,2)")]
        public decimal FinalAmount { get; private set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public int? CustomerId { get; set; }

        public int UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Hoàn tất";

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
