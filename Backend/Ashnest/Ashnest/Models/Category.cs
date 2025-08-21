using System.ComponentModel.DataAnnotations;

namespace Ashnest.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public byte[] CategoryImage { get; set; }

        public string ImageMimeType { get; set; }

        public int? ParentCategoryId { get; set; }

        // Navigation properties
        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
