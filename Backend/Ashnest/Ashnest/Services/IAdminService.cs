using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IAdminService
    {
        Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<InventoryReportDto> GetInventoryReportAsync();
        Task<CustomerReportDto> GetCustomerReportAsync();
    }
}
