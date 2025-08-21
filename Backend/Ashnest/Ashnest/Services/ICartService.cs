using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request);
        Task<CartDto> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemRequest request);
        Task<bool> RemoveFromCartAsync(int userId, int itemId);
        Task<bool> ClearCartAsync(int userId);
    }
}
