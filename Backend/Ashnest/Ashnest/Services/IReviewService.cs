using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(int userId, int productId, CreateReviewRequest request);
        Task<ReviewDto> UpdateReviewAsync(int userId, int reviewId, UpdateReviewRequest request);
        Task<bool> DeleteReviewAsync(int userId, int reviewId);
        Task<List<ReviewDto>> GetProductReviewsAsync(int productId);
        Task<List<ReviewDto>> GetUserReviewsAsync(int userId);
        Task<ReviewDto> GetReviewByIdAsync(int reviewId);
    }
}
