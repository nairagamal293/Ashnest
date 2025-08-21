using Ashnest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ashnest.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("sales-report")]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var report = await _adminService.GetSalesReportAsync(startDate, endDate);
            return Ok(report);
        }

        [HttpGet("inventory-report")]
        public async Task<IActionResult> GetInventoryReport()
        {
            var report = await _adminService.GetInventoryReportAsync();
            return Ok(report);
        }

        [HttpGet("customer-report")]
        public async Task<IActionResult> GetCustomerReport()
        {
            var report = await _adminService.GetCustomerReportAsync();
            return Ok(report);
        }
    }
}
