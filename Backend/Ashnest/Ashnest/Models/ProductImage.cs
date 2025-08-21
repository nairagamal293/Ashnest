using System.ComponentModel.DataAnnotations;

namespace Ashnest.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public string MimeType { get; set; }

        public bool IsPrimary { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
    }
}
