using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.OrderTotal - (o.DiscountAmount ?? 0));
            var totalProductsSold = orders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity));

            // Category sales
            var categorySales = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key,
                    ItemsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .ToList();

            // Daily sales
            var dailySales = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    Orders = g.Count(),
                    Revenue = g.Sum(o => o.OrderTotal - (o.DiscountAmount ?? 0))
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Total customers who placed orders
            var totalCustomers = orders.Select(o => o.UserId).Distinct().Count();

            return new SalesReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                TotalRevenue = totalRevenue,
                TotalCustomers = totalCustomers,
                TotalProductsSold = totalProductsSold,
                CategorySales = categorySales,
                DailySales = dailySales
            };
        }

        public async Task<InventoryReportDto> GetInventoryReportAsync()
        {
            var products = await _context.Products.ToListAsync();

            var outOfStock = products.Count(p => p.StockQuantity == 0);
            var lowStock = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10);

            var lowStockItems = products
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= 10)
                .Select(p => new LowStockItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CurrentStock = p.StockQuantity
                })
                .ToList();

            return new InventoryReportDto
            {
                TotalProducts = products.Count,
                OutOfStockProducts = outOfStock,
                LowStockProducts = lowStock,
                LowStockItems = lowStockItems
            };
        }

        public async Task<CustomerReportDto> GetCustomerReportAsync()
        {
            var customers = await _context.Users
                .Where(u => u.Role == UserRole.Customer)
                .ToListAsync();

            var thisMonth = DateTime.UtcNow.Month;
            var thisYear = DateTime.UtcNow.Year;

            var newCustomersThisMonth = customers
                .Count(u => u.CreatedAt.Month == thisMonth && u.CreatedAt.Year == thisYear);

            // Get active customers (placed order in last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var activeCustomerIds = await _context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .Select(o => o.UserId)
                .Distinct()
                .ToListAsync();

            var activeCustomers = customers.Count(u => activeCustomerIds.Contains(u.Id));

            // Top customers by spending
            var topCustomers = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { o.UserId, o.User.FirstName, o.User.LastName })
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key.UserId,
                    CustomerName = $"{g.Key.FirstName} {g.Key.LastName}",
                    OrdersCount = g.Count(),
                    TotalSpent = g.Sum(o => o.OrderTotal - (o.DiscountAmount ?? 0))
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToListAsync();

            return new CustomerReportDto
            {
                TotalCustomers = customers.Count,
                NewCustomersThisMonth = newCustomersThisMonth,
                ActiveCustomers = activeCustomers,
                TopCustomers = topCustomers
            };
        }
    }
}
