using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<List<OrderDto>> GetUserOrdersAsync(int userId);
        Task<OrderDto> GetOrderByIdAsync(int userId, int orderId);
        Task<List<OrderDto>> GetAllOrdersAsync(string status = null);
        Task<OrderDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
        Task<bool> CancelOrderAsync(int userId, int orderId);
    }
}
