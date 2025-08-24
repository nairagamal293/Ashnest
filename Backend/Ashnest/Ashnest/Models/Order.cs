using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ashnest.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int AddressId { get; set; }
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 10).ToUpper();
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderTotal { get; set; }
        public decimal ProductDiscountAmount { get; set; } // Added this
        public decimal? CouponDiscountAmount { get; set; } // Added this
        public decimal FinalAmount { get; set; } // Added this
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; }
        [MaxLength(500)]
        public string? ShippingNotes { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        // Coupon details
        public string? CouponCode { get; set; }
        public decimal? CouponDiscountPercentage { get; set; } // Added this

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Address Address { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public enum PaymentMethod
    {
        CashOnDelivery,
        CreditCard,
        Wallet
    }
}
