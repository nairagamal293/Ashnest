namespace Ashnest.DTOs
{
    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProductsSold { get; set; }
        public List<CategorySalesDto> CategorySales { get; set; }
        public List<DailySalesDto> DailySales { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; }
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    public class InventoryReportDto
    {
        public int TotalProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public List<LowStockItemDto> LowStockItems { get; set; }
    }

    public class LowStockItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumThreshold { get; set; } = 10;
    }

    public class CustomerReportDto
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int ActiveCustomers { get; set; }
        public List<TopCustomerDto> TopCustomers { get; set; }
    }

    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
