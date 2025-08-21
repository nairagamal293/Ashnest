using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync(string status = null, int? categoryId = null, string search = null);
        Task<ProductDto> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductRequest request);
        Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request);
        Task<bool> DeleteProductAsync(int id);
        Task<List<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<List<ProductDto>> SearchProductsAsync(string searchTerm);
    }
}
