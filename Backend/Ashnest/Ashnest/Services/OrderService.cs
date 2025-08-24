using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ashnest.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;

        public OrderService(ApplicationDbContext context, ICartService cartService, ICouponService couponService)
        {
            _context = context;
            _cartService = cartService;
            _couponService = couponService;
        }

        public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            // Get user's cart
            var cart = await _cartService.GetCartAsync(userId);
            if (cart == null || !cart.Items.Any())
            {
                throw new Exception("Cart is empty");
            }

            // Verify address belongs to user
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId);
            if (address == null)
            {
                throw new Exception("Address not found");
            }

            // Validate payment method
            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            {
                throw new Exception("Invalid payment method");
            }

            // Ashnest/Services/OrderService.cs (update CreateOrderAsync method)
            decimal orderTotal = 0;
            decimal productDiscountAmount = 0;
            List<OrderItem> orderItems = new List<OrderItem>();

            // Calculate order total with product/category discounts
            foreach (var item in cart.Items)
            {
                // Get product with discount information
                var product = await _context.Products
                    .Include(p => p.Discounts)
                    .Include(p => p.Category)
                    .ThenInclude(c => c.Discounts)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                {
                    throw new Exception($"Product with ID {item.ProductId} not found");
                }

                decimal unitPrice = product.Price;
                decimal? discountedPrice = null;
                decimal itemDiscountAmount = 0;
                DiscountDto discountDto = null;

                // Check for active product-specific discount
                var productDiscount = product.Discounts?
                    .FirstOrDefault(d => d.IsActive &&
                                       DateTime.UtcNow >= d.StartDate &&
                                       DateTime.UtcNow <= d.EndDate);

                if (productDiscount != null)
                {
                    discountedPrice = product.Price * (1 - (productDiscount.DiscountPercentage / 100));
                    unitPrice = discountedPrice.Value;
                    itemDiscountAmount = (product.Price - unitPrice) * item.Quantity;
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
                    var categoryDiscount = product.Category?.Discounts?
                        .FirstOrDefault(d => d.IsActive &&
                                           DateTime.UtcNow >= d.StartDate &&
                                           DateTime.UtcNow <= d.EndDate);

                    if (categoryDiscount != null)
                    {
                        discountedPrice = product.Price * (1 - (categoryDiscount.DiscountPercentage / 100));
                        unitPrice = discountedPrice.Value;
                        itemDiscountAmount = (product.Price - unitPrice) * item.Quantity;
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

                productDiscountAmount += itemDiscountAmount;
                orderTotal += unitPrice * item.Quantity;

                // Create order item
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price, // Store original price
                    DiscountedUnitPrice = discountedPrice // Store discounted price if applicable
                };
                orderItems.Add(orderItem);

                // Update product stock
                product.StockQuantity -= item.Quantity;
                _context.Products.Update(product);
            }

            // Apply coupon discount on the already discounted total
            decimal couponDiscountAmount = 0;
            decimal? couponDiscountPercentage = null;

            if (!string.IsNullOrEmpty(request.CouponCode))
            {
                var couponValidation = await _couponService.ValidateCouponAsync(request.CouponCode, orderTotal);
                if (!couponValidation.IsValid)
                {
                    throw new Exception(couponValidation.Message);
                }
                couponDiscountAmount = couponValidation.DiscountAmount;
                couponDiscountPercentage = couponValidation.DiscountPercentage;

                // Record coupon usage
                await _couponService.RecordCouponUsageAsync(request.CouponCode);
            }

            // Calculate final amount
            decimal finalAmount = orderTotal - couponDiscountAmount;

            // Create order
            var order = new Order
            {
                UserId = userId,
                AddressId = request.AddressId,
                PaymentMethod = paymentMethod,
                ShippingNotes = request.ShippingNotes,
                OrderTotal = orderTotal,
                ProductDiscountAmount = productDiscountAmount,
                CouponCode = request.CouponCode,
                CouponDiscountPercentage = couponDiscountPercentage,
                CouponDiscountAmount = couponDiscountAmount,
                FinalAmount = finalAmount,
                Status = OrderStatus.Pending
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order items to the database
            foreach (var item in orderItems)
            {
                item.OrderId = order.Id;
                _context.OrderItems.Add(item);
            }

            await _context.SaveChangesAsync();

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            return await GetOrderByIdAsync(userId, order.Id);
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders.Select(o => MapToDto(o)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserOrdersAsync: {ex.Message}");
                throw new Exception("Failed to retrieve orders. Please try again later.");
            }
        }

        public async Task<OrderDto> GetOrderByIdAsync(int userId, int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(o => o.Id == orderId && (o.UserId == userId || userId == 0)); // userId=0 for admin

                if (order == null)
                {
                    throw new Exception("Order not found");
                }

                return MapToDto(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrderByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync(string status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
            {
                query = query.Where(o => o.Status == statusEnum);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return orders.Select(o => MapToDto(o)).ToList();
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                throw new Exception("Order not found");
            }

            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var statusEnum))
            {
                throw new Exception("Invalid status");
            }

            order.Status = statusEnum;

            // Update timestamps
            if (statusEnum == OrderStatus.Shipped)
            {
                order.ShippedDate = DateTime.UtcNow;
            }
            else if (statusEnum == OrderStatus.Delivered)
            {
                order.DeliveredDate = DateTime.UtcNow;
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return await GetOrderByIdAsync(0, orderId); // userId=0 for admin
        }

        public async Task<bool> CancelOrderAsync(int userId, int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                throw new Exception("Order not found");
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new Exception("Only pending orders can be cancelled");
            }

            // Restore product stock
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _context.Products.Update(product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return true;
        }

        private OrderDto MapToDto(Order order)
        {
            try
            {
                var orderItems = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    DiscountedUnitPrice = oi.DiscountedUnitPrice,
                    TotalPrice = (oi.DiscountedUnitPrice ?? oi.UnitPrice) * oi.Quantity,
                    ProductImage = oi.Product?.ProductImages?.FirstOrDefault(pi => pi.IsPrimary)?.ImageData,
                    ImageMimeType = oi.Product?.ProductImages?.FirstOrDefault(pi => pi.IsPrimary)?.MimeType
                }).ToList() ?? new List<OrderItemDto>();

                return new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    UserName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}" : "Unknown User",
                    ShippingAddress = order.Address != null ? new AddressDto
                    {
                        Id = order.Address.Id,
                        FullName = order.Address.FullName,
                        PhoneNumber = order.Address.PhoneNumber,
                        Street = order.Address.Street,
                        City = order.Address.City,
                        Region = order.Address.Region,
                        PostalCode = order.Address.PostalCode,
                        Country = order.Address.Country,
                        IsDefault = order.Address.IsDefault
                    } : null,
                    OrderItems = orderItems,
                    Subtotal = orderItems.Sum(i => i.UnitPrice * i.Quantity),
                    ProductDiscountAmount = order.ProductDiscountAmount,
                    CouponCode = order.CouponCode,
                    CouponDiscountPercentage = order.CouponDiscountPercentage,
                    CouponDiscountAmount = order.CouponDiscountAmount,
                    OrderTotal = order.OrderTotal,
                    FinalAmount = order.FinalAmount,
                    Status = order.Status.ToString(),
                    PaymentMethod = order.PaymentMethod.ToString(),
                    ShippingNotes = order.ShippingNotes,
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MapToDto: {ex.Message}");
                throw new Exception("Failed to process order data.");
            }
        }
    }
}