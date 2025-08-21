using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

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

            decimal discountAmount = 0;
            decimal discountPercentage = 0;

            // Apply coupon if provided
            if (!string.IsNullOrEmpty(request.CouponCode))
            {
                var couponValidation = await _couponService.ValidateCouponAsync(request.CouponCode, cart.TotalAmount);

                if (!couponValidation.IsValid)
                {
                    throw new Exception(couponValidation.Message);
                }

                discountPercentage = couponValidation.DiscountPercentage;
                discountAmount = couponValidation.DiscountAmount;
            }

            // Create order
            var order = new Order
            {
                UserId = userId,
                AddressId = request.AddressId,
                PaymentMethod = paymentMethod,
                ShippingNotes = request.ShippingNotes,
                OrderTotal = cart.TotalAmount,
                CouponCode = request.CouponCode,
                DiscountPercentage = discountPercentage,
                DiscountAmount = discountAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items
            foreach (var item in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.ProductPrice
                };

                _context.OrderItems.Add(orderItem);

                // Update product stock
                var product = await _context.Products.FindAsync(item.ProductId);
                product.StockQuantity -= item.Quantity;
                _context.Products.Update(product);
            }

            // Update coupon usage if applied
            if (!string.IsNullOrEmpty(request.CouponCode))
            {
                await _couponService.RecordCouponUsageAsync(request.CouponCode);
            }

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            await _context.SaveChangesAsync();

            return await GetOrderByIdAsync(userId, order.Id);
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDto(o)).ToList();
        }

        public async Task<OrderDto> GetOrderByIdAsync(int userId, int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && (o.UserId == userId || userId == 0)); // userId=0 for admin

            if (order == null)
            {
                throw new Exception("Order not found");
            }

            return MapToDto(order);
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
                product.StockQuantity += item.Quantity;
                _context.Products.Update(product);
            }

            order.Status = OrderStatus.Cancelled;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return true;
        }

        private OrderDto MapToDto(Order order)
        {
            var orderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.Quantity * oi.UnitPrice
            }).ToList();

            var finalAmount = order.OrderTotal - (order.DiscountAmount ?? 0);

            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                UserName = $"{order.User.FirstName} {order.User.LastName}",
                ShippingAddress = new AddressDto
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
                },
                OrderItems = orderItems,
                OrderTotal = order.OrderTotal,
                Status = order.Status.ToString(),
                PaymentMethod = order.PaymentMethod.ToString(),
                ShippingNotes = order.ShippingNotes,
                OrderDate = order.OrderDate,
                ShippedDate = order.ShippedDate,
                DeliveredDate = order.DeliveredDate,
                CouponCode = order.CouponCode,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = finalAmount
            };
        }
    }
}
