using Ashnest.DTOs;
using Ashnest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ashnest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountService _discountService;

        public DiscountController(IDiscountService discountService)
        {
            _discountService = discountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiscounts([FromQuery] bool? active = null)
        {
            var discounts = await _discountService.GetAllDiscountsAsync(active);
            return Ok(discounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscount(int id)
        {
            try
            {
                var discount = await _discountService.GetDiscountByIdAsync(id);
                return Ok(discount);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductDiscounts(int productId)
        {
            var discounts = await _discountService.GetProductDiscountsAsync(productId);
            return Ok(discounts);
        }

        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryDiscounts(int categoryId)
        {
            var discounts = await _discountService.GetCategoryDiscountsAsync(categoryId);
            return Ok(discounts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request)
        {
            try
            {
                var discount = await _discountService.CreateDiscountAsync(request);
                return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, discount);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] UpdateDiscountRequest request)
        {
            try
            {
                var discount = await _discountService.UpdateDiscountAsync(id, request);
                return Ok(discount);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            try
            {
                var result = await _discountService.DeleteDiscountAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Discount not found" });
                }
                return Ok(new { message = "Discount deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

}
