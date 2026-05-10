using ParkEase.AuthService.Entities;

namespace ParkEase.AuthService.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUserIdAsync(int userId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<List<User>> FindAllByRoleAsync(string role);
    Task<User?> FindByVehiclePlateAsync(string plate);
    Task<User?> FindByPhoneAsync(string phone);
    Task<User?> FindByOAuthAsync(string provider, string providerId);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteByUserIdAsync(int userId);
    Task<List<User>> GetAllAsync();
}
