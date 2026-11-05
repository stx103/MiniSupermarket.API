using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MiniSupermarket.API.Models
{
    // [Tiêu chí 18] Entity Customer ánh xạ bảng Customers: thông tin khách hàng thành viên, địa chỉ và điểm tích lũy.
    [Table("Customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }
        // NULL cho dữ liệu cũ không biết ngày đăng ký, tránh đếm nhầm là khách mới hôm nay.
        public DateTime? CreatedAt { get; set; }

        [Required(ErrorMessage = "Tên khách hàng không được để trống!")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống!")]
        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ khách hàng.")]
        [StringLength(200)]
        [RegularExpression(CustomerAddressFormat.Pattern, ErrorMessage = CustomerAddressFormat.ErrorMessage)]
        public string Address { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int RewardPoints { get; set; } = 0;

        [StringLength(50)]
        public string MembershipRank { get; set; } = "Chuẩn";

        [JsonIgnore]
        public virtual ICollection<Order>? Orders { get; set; }
    }
}
