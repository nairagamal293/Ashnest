using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Services
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CouponDto> CreateCouponAsync(CreateCouponRequest request)
        {
            // Check if coupon code already exists
            if (await _context.Coupons.AnyAsync(c => c.CouponCode == request.CouponCode))
            {
                throw new Exception("Coupon code already exists");
            }

            var coupon = new Coupon
            {
                CouponCode = request.CouponCode,
                DiscountPercentage = request.DiscountPercentage,
                ExpiryDate = request.ExpiryDate,
                UsageLimit = request.UsageLimit,
                MinimumOrderAmount = request.MinimumOrderAmount
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return MapToDto(coupon);
        }

        public async Task<CouponDto> UpdateCouponAsync(int id, UpdateCouponRequest request)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
            {
                throw new Exception("Coupon not found");
            }

            if (!string.IsNullOrEmpty(request.CouponCode) && request.CouponCode != coupon.CouponCode)
            {
                // Check if new code already exists
                if (await _context.Coupons.AnyAsync(c => c.CouponCode == request.CouponCode && c.Id != id))
                {
                    throw new Exception("Coupon code already exists");
                }
                coupon.CouponCode = request.CouponCode;
            }

            if (request.DiscountPercentage.HasValue)
                coupon.DiscountPercentage = request.DiscountPercentage.Value;

            if (request.ExpiryDate.HasValue)
                coupon.ExpiryDate = request.ExpiryDate.Value;

            if (request.IsActive.HasValue)
                coupon.IsActive = request.IsActive.Value;

            if (request.UsageLimit.HasValue)
                coupon.UsageLimit = request.UsageLimit.Value;

            if (request.MinimumOrderAmount.HasValue)
                coupon.MinimumOrderAmount = request.MinimumOrderAmount.Value;

            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();

            return MapToDto(coupon);
        }

        public async Task<bool> DeleteCouponAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
            {
                return false;
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<CouponDto>> GetAllCouponsAsync(bool? active = null)
        {
            var query = _context.Coupons.AsQueryable();

            if (active.HasValue)
            {
                query = query.Where(c => c.IsActive == active.Value);
            }

            var coupons = await query.ToListAsync();
            return coupons.Select(c => MapToDto(c)).ToList();
        }

        public async Task<CouponDto> GetCouponByIdAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
            {
                throw new Exception("Coupon not found");
            }

            return MapToDto(coupon);
        }

        public async Task<CouponDto> GetCouponByCodeAsync(string code)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == code);

            if (coupon == null)
            {
                throw new Exception("Coupon not found");
            }

            return MapToDto(coupon);
        }

        public async Task<CouponValidationResponse> ValidateCouponAsync(string code, decimal orderAmount)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == code && c.IsActive);

            if (coupon == null)
            {
                return new CouponValidationResponse
                {
                    IsValid = false,
                    Message = "Invalid coupon code"
                };
            }

            // Check expiry
            if (coupon.ExpiryDate < DateTime.UtcNow)
            {
                return new CouponValidationResponse
                {
                    IsValid = false,
                    Message = "Coupon has expired"
                };
            }

            // Check usage limit
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                return new CouponValidationResponse
                {
                    IsValid = false,
                    Message = "Coupon usage limit reached"
                };
            }

            // Check minimum order amount
            if (coupon.MinimumOrderAmount.HasValue && orderAmount < coupon.MinimumOrderAmount.Value)
            {
                return new CouponValidationResponse
                {
                    IsValid = false,
                    Message = $"Minimum order amount of {coupon.MinimumOrderAmount.Value:C} required"
                };
            }

            var discountAmount = orderAmount * (coupon.DiscountPercentage / 100);

            return new CouponValidationResponse
            {
                IsValid = true,
                Message = "Coupon applied successfully",
                DiscountPercentage = coupon.DiscountPercentage,
                DiscountAmount = discountAmount
            };
        }

        public async Task RecordCouponUsageAsync(string code)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == code);

            if (coupon != null)
            {
                coupon.UsedCount++;
                _context.Coupons.Update(coupon);
                await _context.SaveChangesAsync();
            }
        }

        private CouponDto MapToDto(Coupon coupon)
        {
            return new CouponDto
            {
                Id = coupon.Id,
                CouponCode = coupon.CouponCode,
                DiscountPercentage = coupon.DiscountPercentage,
                ExpiryDate = coupon.ExpiryDate,
                IsActive = coupon.IsActive,
                UsageLimit = coupon.UsageLimit,
                UsedCount = coupon.UsedCount,
                MinimumOrderAmount = coupon.MinimumOrderAmount
            };
        }
    }
}
