using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IWishlistService
    {
        Task<WishlistDto> GetWishlistAsync(int userId);
        Task<WishlistItemDto> AddToWishlistAsync(int userId, int productId);
        Task<bool> RemoveFromWishlistAsync(int userId, int itemId);
        Task<bool> IsProductInWishlistAsync(int userId, int productId);
    }
}
