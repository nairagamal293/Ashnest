using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _context;

        public DiscountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DiscountDto>> GetAllDiscountsAsync(bool? active = null)
        {
            var query = _context.Discounts
                .Include(d => d.Product)
                .Include(d => d.Category)
                .AsQueryable();

            if (active.HasValue)
            {
                query = query.Where(d => d.IsActive == active.Value);
            }

            var discounts = await query.ToListAsync();
            return discounts.Select(MapToDto).ToList();
        }

        public async Task<DiscountDto> GetDiscountByIdAsync(int id)
        {
            var discount = await _context.Discounts
                .Include(d => d.Product)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                throw new Exception("Discount not found");
            }

            return MapToDto(discount);
        }

        public async Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request)
        {
            // Validate that either ProductId or CategoryId is provided, but not both
            if (request.ProductId.HasValue && request.CategoryId.HasValue)
            {
                throw new Exception("Discount can be applied to either a product or a category, not both");
            }

            if (!request.ProductId.HasValue && !request.CategoryId.HasValue)
            {
                throw new Exception("Discount must be applied to either a product or a category");
            }

            // Validate dates
            if (request.StartDate >= request.EndDate)
            {
                throw new Exception("Start date must be before end date");
            }

            var discount = new Discount
            {
                Name = request.Name,
                Description = request.Description,
                DiscountPercentage = request.DiscountPercentage,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ProductId = request.ProductId,
                CategoryId = request.CategoryId
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return await GetDiscountByIdAsync(discount.Id);
        }

        public async Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                throw new Exception("Discount not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
                discount.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                discount.Description = request.Description;

            if (request.DiscountPercentage.HasValue)
                discount.DiscountPercentage = request.DiscountPercentage.Value;

            if (request.StartDate.HasValue)
                discount.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                discount.EndDate = request.EndDate.Value;

            if (request.IsActive.HasValue)
                discount.IsActive = request.IsActive.Value;

            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();

            return await GetDiscountByIdAsync(id);
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return false;
            }

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DiscountDto>> GetProductDiscountsAsync(int productId)
        {
            var discounts = await _context.Discounts
                .Where(d => d.ProductId == productId && d.IsActive)
                .ToListAsync();

            return discounts.Select(MapToDto).ToList();
        }

        public async Task<List<DiscountDto>> GetCategoryDiscountsAsync(int categoryId)
        {
            var discounts = await _context.Discounts
                .Where(d => d.CategoryId == categoryId && d.IsActive)
                .ToListAsync();

            return discounts.Select(MapToDto).ToList();
        }

        private DiscountDto MapToDto(Discount discount)
        {
            return new DiscountDto
            {
                Id = discount.Id,
                Name = discount.Name,
                Description = discount.Description,
                DiscountPercentage = discount.DiscountPercentage,
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                IsActive = discount.IsActive,
                ProductId = discount.ProductId,
                ProductName = discount.Product?.Name,
                CategoryId = discount.CategoryId,
                CategoryName = discount.Category?.Name
            };
        }
    }

}
