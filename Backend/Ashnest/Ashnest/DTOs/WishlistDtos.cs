namespace Ashnest.DTOs
{
    // In Ashnest/DTOs/WishlistDto.cs

    public class WishlistItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal? DiscountedPrice { get; set; } // Add this property
        public byte[] PrimaryImage { get; set; }
        public string ImageMimeType { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class WishlistDto
    {
        public int UserId { get; set; }
        public List<WishlistItemDto> Items { get; set; }
        public int TotalItems { get; set; }
    }
}
