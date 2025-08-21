using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ashnest.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CouponCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumOrderAmount { get; set; }
    }
}
