using Ashnest.Data;
using Ashnest.DTOs;
using Ashnest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Ashnest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    Street = a.Street,
                    City = a.City,
                    Region = a.Region,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound(new { message = "Address not found" });
            }

            return Ok(new AddressDto
            {
                Id = address.Id,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                Street = address.Street,
                City = address.City,
                Region = address.Region,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // If setting as default, remove default from other addresses
            if (request.IsDefault)
            {
                var defaultAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                foreach (var addr in defaultAddresses)
                {
                    addr.IsDefault = false;
                    _context.Addresses.Update(addr);
                }
            }

            var address = new Address
            {
                UserId = userId,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Street = request.Street,
                City = request.City,
                Region = request.Region,
                PostalCode = request.PostalCode,
                Country = request.Country,
                IsDefault = request.IsDefault
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, new AddressDto
            {
                Id = address.Id,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                Street = address.Street,
                City = address.City,
                Region = address.Region,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateAddressRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound(new { message = "Address not found" });
            }

            // If setting as default, remove default from other addresses
            if (request.IsDefault.HasValue && request.IsDefault.Value)
            {
                var defaultAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != id)
                    .ToListAsync();

                foreach (var addr in defaultAddresses)
                {
                    addr.IsDefault = false;
                    _context.Addresses.Update(addr);
                }
            }

            if (!string.IsNullOrEmpty(request.FullName))
                address.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                address.PhoneNumber = request.PhoneNumber;

            if (!string.IsNullOrEmpty(request.Street))
                address.Street = request.Street;

            if (!string.IsNullOrEmpty(request.City))
                address.City = request.City;

            if (!string.IsNullOrEmpty(request.Region))
                address.Region = request.Region;

            if (!string.IsNullOrEmpty(request.PostalCode))
                address.PostalCode = request.PostalCode;

            if (!string.IsNullOrEmpty(request.Country))
                address.Country = request.Country;

            if (request.IsDefault.HasValue)
                address.IsDefault = request.IsDefault.Value;

            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();

            return Ok(new AddressDto
            {
                Id = address.Id,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                Street = address.Street,
                City = address.City,
                Region = address.Region,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound(new { message = "Address not found" });
            }

            // Check if address is used in any orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.AddressId == id);

            if (hasOrders)
            {
                return BadRequest(new { message = "Cannot delete address used in orders" });
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Address deleted successfully" });
        }
    }
}
