using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Set default values
            user.UserId = Guid.NewGuid();
            user.CreatedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsActive = true;
            user.IsDeleted = false;
            user.AccountType ??= "free";
            user.EmailVerified = false;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            user.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsDeleted = true;
            user.DeletedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<bool> UserExistsByUsernameAsync(string username)
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTimeOffset.UtcNow;
                user.LastActivityAt = DateTimeOffset.UtcNow;
                await UpdateUserAsync(user);
            }
        }

        public async Task UpdateUserActivityAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.LastActivityAt = DateTimeOffset.UtcNow;
                await UpdateUserAsync(user);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(int page = 1, int pageSize = 20)
        {
            return await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users
                .CountAsync(u => !u.IsDeleted);
        }

        public async Task<bool> VerifyEmailAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.EmailVerified = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateUserPreferencesAsync(Guid userId, string preferences)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.Preferences = preferences;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}