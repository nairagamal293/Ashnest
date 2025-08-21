using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public CategoryService(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Where(c => c.ParentCategoryId == null)
                .ToListAsync();

            return categories.Select(c => MapToDto(c)).ToList();
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                throw new Exception("Category not found");
            }

            return MapToDto(category);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
        {
            // Validate parent category
            if (request.ParentCategoryId.HasValue)
            {
                var parentCategory = await _context.Categories.FindAsync(request.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    throw new Exception("Parent category not found");
                }
            }

            byte[] imageData = null;
            string mimeType = null;

            if (request.ImageFile != null)
            {
                if (!_imageService.IsValidImage(request.ImageFile))
                {
                    throw new Exception("Invalid image file");
                }

                imageData = await _imageService.ConvertImageToByteArrayAsync(request.ImageFile);
                mimeType = _imageService.GetImageMimeType(request.ImageFile.FileName);
            }

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                ParentCategoryId = request.ParentCategoryId,
                CategoryImage = imageData,
                ImageMimeType = mimeType
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await GetCategoryByIdAsync(category.Id);
        }

        public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                throw new Exception("Category not found");
            }

            // Validate parent category
            if (request.ParentCategoryId.HasValue && request.ParentCategoryId.Value != category.ParentCategoryId)
            {
                if (request.ParentCategoryId.Value == id)
                {
                    throw new Exception("Category cannot be its own parent");
                }

                var parentCategory = await _context.Categories.FindAsync(request.ParentCategoryId.Value);
                if (parentCategory == null)
                {
                    throw new Exception("Parent category not found");
                }

                category.ParentCategoryId = request.ParentCategoryId;
            }

            if (!string.IsNullOrEmpty(request.Name))
                category.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                category.Description = request.Description;

            if (request.ImageFile != null)
            {
                if (!_imageService.IsValidImage(request.ImageFile))
                {
                    throw new Exception("Invalid image file");
                }

                category.CategoryImage = await _imageService.ConvertImageToByteArrayAsync(request.ImageFile);
                category.ImageMimeType = _imageService.GetImageMimeType(request.ImageFile.FileName);
            }

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return await GetCategoryByIdAsync(category.Id);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return false;
            }

            if (category.SubCategories.Any() || category.Products.Any())
            {
                throw new Exception("Cannot delete category with subcategories or products");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<CategoryDto>> GetSubCategoriesAsync(int parentCategoryId)
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.ParentCategoryId == parentCategoryId)
                .ToListAsync();

            return categories.Select(c => MapToDto(c)).ToList();
        }

        private CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CategoryImage = category.CategoryImage,
                ImageMimeType = category.ImageMimeType,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.Name,
                SubCategories = category.SubCategories?.Select(MapToDto).ToList() ?? new List<CategoryDto>()
            };
        }
    }

}
