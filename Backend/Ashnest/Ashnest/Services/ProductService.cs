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
                    .ThenInclude(c => c.Discounts) // Include category discounts
                .Include(p => p.Discounts) // Include product discounts
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
                    .ThenInclude(c => c.Discounts) // Include category discounts
                .Include(p => p.Discounts) // Include product discounts
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                throw new Exception("Product not found");
            }

            return MapToDto(product);
        }


        // In Ashnest/Services/ProductService.cs
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

            // Add product images - FIXED: Make this optional
            if (request.ImageFiles != null && request.ImageFiles.Any())
            {
                foreach (var imageFile in request.ImageFiles)
                {
                    // Skip invalid images but don't throw an exception
                    if (!_imageService.IsValidImage(imageFile))
                    {
                        continue;
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


        // In Ashnest/Services/ProductService.cs
        // In Ashnest/Services/ProductService.cs
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

            // Get all product images for this product
            var existingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == id)
                .ToListAsync();

            // Process image updates
            if (request.ImageUpdates != null && request.ImageUpdates.Any())
            {
                // Get IDs of images to remove
                var imageIdsToRemove = request.ImageUpdates
                    .Where(u => u.Remove)
                    .Select(u => u.Id)
                    .ToList();

                // Remove images marked for removal
                foreach (var imageId in imageIdsToRemove)
                {
                    var image = existingImages.FirstOrDefault(img => img.Id == imageId);
                    if (image != null)
                    {
                        _context.ProductImages.Remove(image);
                        existingImages.Remove(image);
                    }
                }

                // Get the ID of the image to set as primary
                var primaryImageId = request.ImageUpdates
                    .FirstOrDefault(u => !u.Remove && u.IsPrimary)?.Id;

                // If a primary image is specified, set it
                if (primaryImageId.HasValue)
                {
                    var primaryImage = existingImages.FirstOrDefault(img => img.Id == primaryImageId.Value);
                    if (primaryImage != null)
                    {
                        // Set all images to not primary first
                        foreach (var img in existingImages)
                        {
                            img.IsPrimary = false;
                        }
                        // Set the specified image as primary
                        primaryImage.IsPrimary = true;
                    }
                }
                else
                {
                    // If no primary image is specified, check if there's already a primary image
                    if (!existingImages.Any(img => img.IsPrimary) && existingImages.Any())
                    {
                        // If not, set the first image as primary
                        existingImages.First().IsPrimary = true;
                    }
                }

                // Update all images
                foreach (var image in existingImages)
                {
                    _context.ProductImages.Update(image);
                }
            }

            // Add new images
            if (request.NewImageFiles != null && request.NewImageFiles.Any())
            {
                foreach (var imageFile in request.NewImageFiles)
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageData = await _imageService.ConvertImageToByteArrayAsync(imageFile);
                        var mimeType = _imageService.GetImageMimeType(imageFile.FileName);
                        var productImage = new ProductImage
                        {
                            ProductId = id,
                            ImageData = imageData,
                            MimeType = mimeType,
                            IsPrimary = false // Default to not primary
                        };
                        _context.ProductImages.Add(productImage);
                        existingImages.Add(productImage);
                    }
                }
            }

            // Ensure there's always a primary image
            if (existingImages.Any() && !existingImages.Any(pi => pi.IsPrimary))
            {
                existingImages.First().IsPrimary = true;
                _context.ProductImages.Update(existingImages.First());
            }

            await _context.SaveChangesAsync();
            return await GetProductByIdAsync(id);
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

        // Ashnest/Services/ProductService.cs (update MapToDto method)
        // Ashnest/Services/ProductService.cs (update MapToDto method)
        private ProductDto MapToDto(Product product)
        {
            var averageRating = product.Reviews?.Any() == true
                ? product.Reviews.Average(r => r.Rating)
                : 0;

            // Get current UTC date
            var now = DateTime.UtcNow;

            // Find active product-specific discount
            var productDiscount = product.Discounts?
                .FirstOrDefault(d => d.IsActive &&
                                   now >= d.StartDate &&
                                   now <= d.EndDate);

            // Find active category discount if no product discount
            Discount categoryDiscount = null;
            if (productDiscount == null && product.Category?.Discounts != null)
            {
                categoryDiscount = product.Category.Discounts
                    .FirstOrDefault(d => d.IsActive &&
                                       now >= d.StartDate &&
                                       now <= d.EndDate);
            }

            // Determine which discount to apply
            Discount activeDiscount = productDiscount ?? categoryDiscount;

            // Calculate discounted price
            decimal? discountedPrice = null;
            DiscountDto discountDto = null;

            if (activeDiscount != null)
            {
                discountedPrice = product.Price * (1 - (activeDiscount.DiscountPercentage / 100));
                discountDto = new DiscountDto
                {
                    Id = activeDiscount.Id,
                    Name = activeDiscount.Name,
                    Description = activeDiscount.Description,
                    DiscountPercentage = activeDiscount.DiscountPercentage,
                    StartDate = activeDiscount.StartDate,
                    EndDate = activeDiscount.EndDate,
                    IsActive = activeDiscount.IsActive
                };
            }

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountedPrice = discountedPrice,
                Discount = discountDto,
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
