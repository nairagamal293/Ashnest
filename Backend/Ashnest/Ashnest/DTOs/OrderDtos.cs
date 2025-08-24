using System.ComponentModel.DataAnnotations;

namespace Ashnest.DTOs
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountedUnitPrice { get; set; } // Added this
        public decimal TotalPrice { get; set; }
        public byte[] ProductImage { get; set; } // Added this
        public string ImageMimeType { get; set; } // Added this
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public AddressDto ShippingAddress { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public decimal Subtotal { get; set; } // Added this
        public decimal ProductDiscountAmount { get; set; } // Added this
        public string CouponCode { get; set; }
        public decimal? CouponDiscountPercentage { get; set; } // Added this
        public decimal? CouponDiscountAmount { get; set; } // Added this
        public decimal OrderTotal { get; set; }
        public decimal FinalAmount { get; set; } // Added this
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string ShippingNotes { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }

    public class CreateOrderRequest
    {
        [Required]
        public int AddressId { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string ShippingNotes { get; set; }
        public string CouponCode { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public string Status { get; set; }
    }
}
