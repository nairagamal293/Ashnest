using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Discounts) // Include product discounts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                            .ThenInclude(c => c.Discounts) // Include category discounts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages.Where(pi => pi.IsPrimary))
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Create cart if it doesn't exist
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
                return new CartDto
                {
                    Id = cart.Id,
                    UserId = userId,
                    Items = new List<CartItemDto>(),
                    TotalAmount = 0,
                    TotalItems = 0
                };
            }

            return MapToDto(cart);
        }


        public async Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request)
        {
            // Verify product exists and is active
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.Status == ProductStatus.Active);

            if (product == null)
            {
                throw new Exception("Product not found or not available");
            }

            // Check stock
            if (product.StockQuantity < request.Quantity)
            {
                throw new Exception($"Insufficient stock. Only {product.StockQuantity} available.");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Check if product already in cart
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                _context.CartItems.Update(existingItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemRequest request)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                throw new Exception("Cart item not found");
            }

            if (request.Quantity == 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                // Verify stock
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product.StockQuantity < request.Quantity)
                {
                    throw new Exception($"Insufficient stock. Only {product.StockQuantity} available.");
                }

                cartItem.Quantity = request.Quantity;
                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int itemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return false;
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return false;
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return true;
        }

        // Ashnest/Services/CartService.cs (update MapToDto method)
        // Ashnest/Services/CartService.cs (update MapToDto method)
        private CartDto MapToDto(Cart cart)
        {
            var items = cart.CartItems.Select(ci =>
            {
                decimal unitPrice = ci.Product.Price;
                decimal? discountedPrice = null;
                DiscountDto discountDto = null;

                // Check for active product-specific discount
                var productDiscount = ci.Product.Discounts?
                    .FirstOrDefault(d => d.IsActive &&
                                       DateTime.UtcNow >= d.StartDate &&
                                       DateTime.UtcNow <= d.EndDate);

                if (productDiscount != null)
                {
                    discountedPrice = ci.Product.Price * (1 - (productDiscount.DiscountPercentage / 100));
                    unitPrice = discountedPrice.Value;
                    discountDto = new DiscountDto
                    {
                        Id = productDiscount.Id,
                        Name = productDiscount.Name,
                        DiscountPercentage = productDiscount.DiscountPercentage,
                        StartDate = productDiscount.StartDate,
                        EndDate = productDiscount.EndDate,
                        IsActive = productDiscount.IsActive
                    };
                }
                // Check for active category discount if no product discount
                else
                {
                    var categoryDiscount = ci.Product.Category?.Discounts?
                        .FirstOrDefault(d => d.IsActive &&
                                           DateTime.UtcNow >= d.StartDate &&
                                           DateTime.UtcNow <= d.EndDate);

                    if (categoryDiscount != null)
                    {
                        discountedPrice = ci.Product.Price * (1 - (categoryDiscount.DiscountPercentage / 100));
                        unitPrice = discountedPrice.Value;
                        discountDto = new DiscountDto
                        {
                            Id = categoryDiscount.Id,
                            Name = categoryDiscount.Name,
                            DiscountPercentage = categoryDiscount.DiscountPercentage,
                            StartDate = categoryDiscount.StartDate,
                            EndDate = categoryDiscount.EndDate,
                            IsActive = categoryDiscount.IsActive
                        };
                    }
                }

                return new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductPrice = ci.Product.Price,
                    DiscountedPrice = discountedPrice,
                    Discount = discountDto,
                    Quantity = ci.Quantity,
                    TotalPrice = unitPrice * ci.Quantity,
                    PrimaryImage = ci.Product.ProductImages?.FirstOrDefault()?.ImageData,
                    ImageMimeType = ci.Product.ProductImages?.FirstOrDefault()?.MimeType
                };
            }).ToList();

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = items,
                TotalAmount = items.Sum(i => i.TotalPrice),
                TotalItems = items.Sum(i => i.Quantity)
            };
        }
    }
}
