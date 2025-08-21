using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewDto> CreateReviewAsync(int userId, int productId, CreateReviewRequest request)
        {
            // Verify product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new Exception("Product not found");
            }

            // Check if user has already reviewed this product
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

            if (existingReview != null)
            {
                throw new Exception("You have already reviewed this product");
            }

            // Check if user has purchased this product
            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == productId &&
                                oi.Order.UserId == userId &&
                                oi.Order.Status == OrderStatus.Delivered);

            if (!hasPurchased)
            {
                throw new Exception("You can only review products you have purchased");
            }

            var review = new Review
            {
                UserId = userId,
                ProductId = productId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<ReviewDto> UpdateReviewAsync(int userId, int reviewId, UpdateReviewRequest request)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                throw new Exception("Review not found");
            }

            if (request.Rating.HasValue)
                review.Rating = request.Rating.Value;

            if (!string.IsNullOrEmpty(request.Comment))
                review.Comment = request.Comment;

            review.UpdatedDate = DateTime.UtcNow;

            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<bool> DeleteReviewAsync(int userId, int reviewId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r)).ToList();
        }

        public async Task<List<ReviewDto>> GetUserReviewsAsync(int userId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r)).ToList();
        }

        public async Task<ReviewDto> GetReviewByIdAsync(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                throw new Exception("Review not found");
            }

            return MapToDto(review);
        }

        private ReviewDto MapToDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = $"{review.User.FirstName} {review.User.LastName}",
                ProductId = review.ProductId,
                ProductName = review.Product.Name,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedDate = review.CreatedDate,
                UpdatedDate = review.UpdatedDate
            };
        }
    }
}
