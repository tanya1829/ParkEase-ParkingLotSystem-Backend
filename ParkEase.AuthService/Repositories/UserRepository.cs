using Microsoft.EntityFrameworkCore;
using ParkEase.AuthService.Data;
using ParkEase.AuthService.Entities;
using ParkEase.AuthService.Interfaces;

namespace ParkEase.AuthService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> FindByUserIdAsync(int userId) =>
        await _context.Users.FindAsync(userId);

    public async Task<bool> ExistsByEmailAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLower());

    public async Task<List<User>> FindAllByRoleAsync(string role) =>
        await _context.Users.Where(u => u.Role == role).ToListAsync();

    public async Task<User?> FindByVehiclePlateAsync(string plate) =>
        await _context.Users.FirstOrDefaultAsync(u => u.VehiclePlate == plate);

    public async Task<User?> FindByPhoneAsync(string phone) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);

    public async Task<User?> FindByOAuthAsync(string provider, string providerId) =>
        await _context.Users.FirstOrDefaultAsync(u =>
            u.OAuthProvider == provider && u.OAuthProviderId == providerId);

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLower();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteByUserIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllAsync() =>
        await _context.Users.ToListAsync();
}
