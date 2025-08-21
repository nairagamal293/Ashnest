using System.ComponentModel.DataAnnotations;

namespace Ashnest.DTOs
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string CouponCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
    }

    public class CreateCouponRequest
    {
        [Required]
        public string CouponCode { get; set; }

        [Required]
        [Range(1, 100)]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public int? UsageLimit { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
    }

    public class UpdateCouponRequest
    {
        public string CouponCode { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
    }

    public class ApplyCouponRequest
    {
        [Required]
        public string CouponCode { get; set; }

        [Required]
        public decimal OrderAmount { get; set; }
    }

    public class CouponValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
    }

}
