using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public ProductService(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync(string status = null, int? categoryId = null, string search = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProductStatus>(status, true, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            var products = await query.ToListAsync();
            return products.Select(p => MapToDto(p)).ToList();
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            return MapToDto(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
        {
            // Validate category
            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
            {
                throw new Exception("Category not found");
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                Status = ProductStatus.Active
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Add product images
            if (request.ImageFiles != null && request.ImageFiles.Any())
            {
                foreach (var imageFile in request.ImageFiles)
                {
                    if (!_imageService.IsValidImage(imageFile))
                    {
                        continue; // Skip invalid images
                    }

                    var imageData = await _imageService.ConvertImageToByteArrayAsync(imageFile);
                    var mimeType = _imageService.GetImageMimeType(imageFile.FileName);

                    var productImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageData = imageData,
                        MimeType = mimeType,
                        IsPrimary = false // First image will be primary if none set
                    };

                    _context.ProductImages.Add(productImage);
                }

                await _context.SaveChangesAsync();

                // Set first image as primary if no primary images
                if (!_context.ProductImages.Any(pi => pi.ProductId == product.Id && pi.IsPrimary))
                {
                    var firstImage = _context.ProductImages
                        .First(pi => pi.ProductId == product.Id);

                    firstImage.IsPrimary = true;
                    _context.ProductImages.Update(firstImage);
                    await _context.SaveChangesAsync();
                }
            }

            return await GetProductByIdAsync(product.Id);
        }

        public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
                product.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                product.Description = request.Description;

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.StockQuantity.HasValue)
                product.StockQuantity = request.StockQuantity.Value;

            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(request.CategoryId.Value);
                if (category == null)
                {
                    throw new Exception("Category not found");
                }
                product.CategoryId = request.CategoryId.Value;
            }

            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProductStatus>(request.Status, true, out var statusEnum))
                product.Status = statusEnum;

            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            // Add new images if provided
            if (request.ImageFiles != null && request.ImageFiles.Any())
            {
                foreach (var imageFile in request.ImageFiles)
                {
                    if (!_imageService.IsValidImage(imageFile))
                    {
                        continue; // Skip invalid images
                    }

                    var imageData = await _imageService.ConvertImageToByteArrayAsync(imageFile);
                    var mimeType = _imageService.GetImageMimeType(imageFile.FileName);

                    var productImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageData = imageData,
                        MimeType = mimeType,
                        IsPrimary = false
                    };

                    _context.ProductImages.Add(productImage);
                }

                await _context.SaveChangesAsync();
            }

            return await GetProductByIdAsync(product.Id);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.CartItems)
                .Include(p => p.OrderItems)
                .Include(p => p.Reviews)
                .Include(p => p.Wishlists)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return false;
            }

            // Check if product is in any orders
            if (product.OrderItems.Any())
            {
                throw new Exception("Cannot delete product that has been ordered");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == categoryId && p.Status == ProductStatus.Active)
                .ToListAsync();

            return products.Select(p => MapToDto(p)).ToList();
        }

        public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p => p.Status == ProductStatus.Active &&
                           (p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)))
                .ToListAsync();

            return products.Select(p => MapToDto(p)).ToList();
        }

        private ProductDto MapToDto(Product product)
        {
            var averageRating = product.Reviews?.Any() == true
                ? product.Reviews.Average(r => r.Rating)
                : 0;

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Status = product.Status.ToString(),
                CreatedAt = product.CreatedAt,
                AverageRating = Math.Round(averageRating, 1),
                ReviewCount = product.Reviews?.Count ?? 0,
                Images = product.ProductImages?.Select(pi => new ProductImageDto
                {
                    Id = pi.Id,
                    ImageData = pi.ImageData,
                    MimeType = pi.MimeType,
                    IsPrimary = pi.IsPrimary
                }).ToList() ?? new List<ProductImageDto>()
            };
        }
    }
}
