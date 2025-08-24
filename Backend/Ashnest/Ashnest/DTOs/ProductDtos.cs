// In Ashnest/DTOs/ProductDto.cs
using System.ComponentModel.DataAnnotations;

namespace Ashnest.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; } // Make sure this exists
        public DiscountDto Discount { get; set; } // Make sure this exists
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductImageDto> Images { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class ProductImageDto
    {
        public int Id { get; set; }
        public byte[] ImageData { get; set; }
        public string MimeType { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class CreateProductRequest
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public List<IFormFile> ImageFiles { get; set; }
    }

    // In Ashnest/DTOs/ProductDto.cs
    // In Ashnest/DTOs/ProductDto.cs
    public class UpdateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public int? CategoryId { get; set; }
        public string Status { get; set; }
        public List<ProductImageUpdate> ImageUpdates { get; set; }
        public List<IFormFile> NewImageFiles { get; set; } = new List<IFormFile>(); // Initialize with empty list
    }

    public class ProductImageUpdate
    {
        public int Id { get; set; }
        public bool Remove { get; set; }
        public bool IsPrimary { get; set; }
    }
}