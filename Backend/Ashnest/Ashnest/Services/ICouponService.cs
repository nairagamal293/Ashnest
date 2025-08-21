using Ashnest.DTOs;

namespace Ashnest.Services
{
    public interface ICouponService
    {
        Task<CouponDto> CreateCouponAsync(CreateCouponRequest request);
        Task<CouponDto> UpdateCouponAsync(int id, UpdateCouponRequest request);
        Task<bool> DeleteCouponAsync(int id);
        Task<List<CouponDto>> GetAllCouponsAsync(bool? active = null);
        Task<CouponDto> GetCouponByIdAsync(int id);
        Task<CouponDto> GetCouponByCodeAsync(string code);
        Task<CouponValidationResponse> ValidateCouponAsync(string code, decimal orderAmount);
        Task RecordCouponUsageAsync(string code);
    }
}
