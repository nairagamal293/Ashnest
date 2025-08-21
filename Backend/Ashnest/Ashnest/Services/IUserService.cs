using Ashnest.DTOs;
using Ashnest.Models;

namespace Ashnest.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int id);
        Task UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(int id);
    }
}
