// In Ashnest/DTOs/CategoryDto.cs
using System.ComponentModel.DataAnnotations;

namespace Ashnest.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] CategoryImage { get; set; }
        public string ImageMimeType { get; set; }
        public int? ParentCategoryId { get; set; }
        public string ParentCategoryName { get; set; }
        public List<CategoryDto> SubCategories { get; set; }
    }

    public class CreateCategoryRequest
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public IFormFile ImageFile { get; set; }
    }

    // In Ashnest/DTOs/CategoryDto.cs
    // In Ashnest/DTOs/CategoryDto.cs
    public class UpdateCategoryRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public IFormFile? ImageFile { get; set; }  // جعلها nullable
        public bool RemoveImage { get; set; }
    }
}