using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MiniSupermarket.API.Models
{
    // [Tiêu chí 18] Entity User ánh xạ bảng Users: tài khoản, vai trò và thông tin nhân viên (1 trong 6 entity nghiệp vụ).
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(254)]
        public string? Email { get; set; }

        [Required]
        [StringLength(100)]
        [JsonIgnore]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Role { get; set; } = "Cashier";

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<Order>? Orders { get; set; }
    }
}
