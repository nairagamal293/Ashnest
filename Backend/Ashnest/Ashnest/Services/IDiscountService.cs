using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IDiscountService
    {
        Task<List<DiscountDto>> GetAllDiscountsAsync(bool? active = null);
        Task<DiscountDto> GetDiscountByIdAsync(int id);
        Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request);
        Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request);
        Task<bool> DeleteDiscountAsync(int id);
        Task<List<DiscountDto>> GetProductDiscountsAsync(int productId);
        Task<List<DiscountDto>> GetCategoryDiscountsAsync(int categoryId);
    }
}
