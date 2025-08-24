using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        // In Ashnest/Services/WishlistService.cs

        public async Task<WishlistDto> GetWishlistAsync(int userId)
        {
            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p.Discounts) // Include product discounts
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                        .ThenInclude(c => c.Discounts) // Include category discounts
                .Include(w => w.Product)
                    .ThenInclude(p => p.ProductImages.Where(pi => pi.IsPrimary))
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            var items = wishlistItems.Select(w => {
                // Get current UTC date
                var now = DateTime.UtcNow;

                // Find active product-specific discount
                var productDiscount = w.Product.Discounts?
                    .FirstOrDefault(d => d.IsActive &&
                                       now >= d.StartDate &&
                                       now <= d.EndDate);

                // Find active category discount if no product discount
                Discount categoryDiscount = null;
                if (productDiscount == null && w.Product.Category?.Discounts != null)
                {
                    categoryDiscount = w.Product.Category.Discounts
                        .FirstOrDefault(d => d.IsActive &&
                                           now >= d.StartDate &&
                                           now <= d.EndDate);
                }

                // Determine which discount to apply
                Discount activeDiscount = productDiscount ?? categoryDiscount;

                // Calculate discounted price
                decimal? discountedPrice = null;
                if (activeDiscount != null)
                {
                    discountedPrice = w.Product.Price * (1 - (activeDiscount.DiscountPercentage / 100));
                }

                return new WishlistItemDto
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    ProductPrice = w.Product.Price,
                    DiscountedPrice = discountedPrice,
                    PrimaryImage = w.Product.ProductImages?.FirstOrDefault()?.ImageData,
                    ImageMimeType = w.Product.ProductImages?.FirstOrDefault()?.MimeType,
                    CreatedDate = w.CreatedDate
                };
            }).ToList();

            return new WishlistDto
            {
                UserId = userId,
                Items = items,
                TotalItems = items.Count
            };
        }

        public async Task<WishlistItemDto> AddToWishlistAsync(int userId, int productId)
        {
            // Verify product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new Exception("Product not found");
            }

            // Check if already in wishlist
            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                throw new Exception("Product already in wishlist");
            }

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = productId
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return new WishlistItemDto
            {
                Id = wishlistItem.Id,
                ProductId = productId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                PrimaryImage = product.ProductImages?.FirstOrDefault(pi => pi.IsPrimary)?.ImageData,
                ImageMimeType = product.ProductImages?.FirstOrDefault(pi => pi.IsPrimary)?.MimeType,
                CreatedDate = wishlistItem.CreatedDate
            };
        }

        public async Task<bool> RemoveFromWishlistAsync(int userId, int itemId)
        {
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == itemId && w.UserId == userId);

            if (wishlistItem == null)
            {
                return false;
            }

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsProductInWishlistAsync(int userId, int productId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }
    }
}
