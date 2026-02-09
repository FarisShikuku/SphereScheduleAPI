using Microsoft.EntityFrameworkCore;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Services
{
    public class UserManagementService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public UserManagementService(ApplicationDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(int page = 1, int pageSize = 20, bool includeDeleted = false)
        {
            var query = _context.Users.AsQueryable();

            if (!includeDeleted)
                query = query.Where(u => !u.IsDeleted);

            return await query
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Validate unique constraints
            if (await UserExistsByEmailAsync(user.Email))
                throw new InvalidOperationException($"User with email {user.Email} already exists");

            if (!string.IsNullOrEmpty(user.Username) && await UserExistsByUsernameAsync(user.Username))
                throw new InvalidOperationException($"User with username {user.Username} already exists");

            user.UserId = Guid.NewGuid();
            user.CreatedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsActive = true;
            user.IsDeleted = false;
            user.EmailVerified = false;
            user.AccountType ??= "free";

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var existing = await GetUserByIdAsync(user.UserId);
            if (existing == null)
                throw new KeyNotFoundException($"User with ID {user.UserId} not found");

            // Check if email is being changed and if it's available
            if (existing.Email != user.Email && await UserExistsByEmailAsync(user.Email))
                throw new InvalidOperationException($"Email {user.Email} is already in use");

            // Check if username is being changed and if it's available
            if (existing.Username != user.Username && await UserExistsByUsernameAsync(user.Username))
                throw new InvalidOperationException($"Username {user.Username} is already in use");

            user.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid userId, bool permanent = false)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            if (permanent)
            {
                _context.Users.Remove(user);
            }
            else
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTimeOffset.UtcNow;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                user.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreUserAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsDeleted);

            if (user == null) return false;

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.IsActive = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateUserCredentialsAsync(string emailOrUsername, string password)
        {
            var user = await GetUserByEmailAsync(emailOrUsername) ??
                       await GetUserByUsernameAsync(emailOrUsername);

            if (user == null || !user.IsActive || user.IsDeleted)
                return false;

            return _passwordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                return false;

            var (hash, salt) = _passwordService.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return false;

            var (hash, salt) = _passwordService.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
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

        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.LastActivityAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLastActivityAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.LastActivityAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockUserAccountAsync(Guid userId, DateTimeOffset? lockoutEnd = null)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.LockoutEnabled = true;
            user.LockoutEnd = lockoutEnd ?? DateTimeOffset.UtcNow.AddHours(24);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockUserAccountAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User> UpdateUserProfileAsync(Guid userId, UpdateUserDto updateDto)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found");

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;

            if (!string.IsNullOrEmpty(updateDto.LastName))
                user.LastName = updateDto.LastName;

            if (!string.IsNullOrEmpty(updateDto.DisplayName))
                user.DisplayName = updateDto.DisplayName;

            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateDto.AvatarUrl))
                user.AvatarUrl = updateDto.AvatarUrl;

            if (updateDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateDto.DateOfBirth.Value;

            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateUserPreferencesAsync(Guid userId, string preferencesJson)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.Preferences = preferencesJson;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAccountAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAccountAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpgradeAccountAsync(Guid userId, string newAccountType, DateTime? subscriptionEndDate = null)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.AccountType = newAccountType;
            user.SubscriptionStartDate = DateTime.UtcNow;
            user.SubscriptionEndDate = subscriptionEndDate;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelSubscriptionAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.AccountType = "free";
            user.SubscriptionEndDate = null;
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

        public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email && !u.IsDeleted);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Username == username && !u.IsDeleted);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        public async Task<int> GetTotalUsersCountAsync(bool includeDeleted = false)
        {
            var query = _context.Users.AsQueryable();

            if (!includeDeleted)
                query = query.Where(u => !u.IsDeleted);

            return await query.CountAsync();
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
        {
            var users = await _context.Users.ToListAsync();

            return new Dictionary<string, int>
            {
                { "total", users.Count },
                { "active", users.Count(u => u.IsActive && !u.IsDeleted) },
                { "inactive", users.Count(u => !u.IsActive && !u.IsDeleted) },
                { "deleted", users.Count(u => u.IsDeleted) },
                { "free", users.Count(u => u.AccountType == "free" && !u.IsDeleted) },
                { "premium", users.Count(u => u.AccountType == "premium" && !u.IsDeleted) },
                { "enterprise", users.Count(u => u.AccountType == "enterprise" && !u.IsDeleted) },
                { "admin", users.Count(u => u.AccountType == "admin" && !u.IsDeleted) },
                { "email_verified", users.Count(u => u.EmailVerified && !u.IsDeleted) },
                { "two_factor_enabled", users.Count(u => u.TwoFactorEnabled && !u.IsDeleted) },
                { "locked", users.Count(u => u.LockoutEnabled && !u.IsDeleted) }
            };
        }

        public async Task<IEnumerable<UserActivityDto>> GetUserActivityLogsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // This would typically query an activity log table
            // For now, returning mock data based on user's last activity
            var user = await GetUserByIdAsync(userId);
            if (user == null) return new List<UserActivityDto>();

            var activities = new List<UserActivityDto>
            {
                new UserActivityDto
                {
                    ActivityType = "login",
                    Timestamp = user.LastLoginAt ?? user.CreatedAt,
                    Details = "User logged in"
                },
                new UserActivityDto
                {
                    ActivityType = "profile_update",
                    Timestamp = user.UpdatedAt,
                    Details = "Profile updated"
                }
            };

            if (startDate.HasValue)
                activities = activities.Where(a => a.Timestamp >= startDate).ToList();

            if (endDate.HasValue)
                activities = activities.Where(a => a.Timestamp <= endDate).ToList();

            return activities.OrderByDescending(a => a.Timestamp);
        }

        public async Task<Dictionary<string, object>> GetUserDashboardStatsAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return new Dictionary<string, object>();

            // Get counts from related tables
            var taskCount = await _context.Tasks
                .CountAsync(t => t.UserId == userId && !t.IsDeleted);

            var appointmentCount = await _context.Appointments
                .CountAsync(a => a.UserId == userId && !a.IsDeleted);

            var completedTasks = await _context.Tasks
                .CountAsync(t => t.UserId == userId && !t.IsDeleted && t.Status == "completed");

            var upcomingAppointments = await _context.Appointments
                .CountAsync(a => a.UserId == userId && !a.IsDeleted &&
                                a.Status == "scheduled" &&
                                a.StartDateTime > DateTimeOffset.UtcNow);

            return new Dictionary<string, object>
            {
                { "user_info", new {
                    display_name = user.DisplayName,
                    account_type = user.AccountType,
                    email_verified = user.EmailVerified,
                    member_since = user.CreatedAt.ToString("yyyy-MM-dd")
                }},
                { "counts", new {
                    total_tasks = taskCount,
                    total_appointments = appointmentCount,
                    completed_tasks = completedTasks,
                    upcoming_appointments = upcomingAppointments
                }},
                { "activity", new {
                    last_login = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm"),
                    last_activity = user.LastActivityAt?.ToString("yyyy-MM-dd HH:mm"),
                    days_active = (DateTimeOffset.UtcNow - user.CreatedAt).Days
                }}
            };
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync(page, pageSize);

            return await _context.Users
                .Where(u => !u.IsDeleted &&
                           (u.Email.Contains(searchTerm) ||
                            u.Username.Contains(searchTerm) ||
                            u.DisplayName.Contains(searchTerm) ||
                            u.FirstName.Contains(searchTerm) ||
                            u.LastName.Contains(searchTerm)))
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByAccountTypeAsync(string accountType, int page = 1, int pageSize = 20)
        {
            return await _context.Users
                .Where(u => !u.IsDeleted && u.AccountType == accountType)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByStatusAsync(bool isActive, int page = 1, int pageSize = 20)
        {
            return await _context.Users
                .Where(u => !u.IsDeleted && u.IsActive == isActive)
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetInactiveUsersAsync(int daysInactive, int page = 1, int pageSize = 20)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysInactive);

            return await _context.Users
                .Where(u => !u.IsDeleted &&
                           u.IsActive &&
                           (u.LastActivityAt == null || u.LastActivityAt < cutoffDate))
                .OrderBy(u => u.LastActivityAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(Guid userId, string accountType)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.AccountType = accountType;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ImpersonateUserAsync(Guid adminUserId, Guid targetUserId)
        {
            var admin = await GetUserByIdAsync(adminUserId);
            var target = await GetUserByIdAsync(targetUserId);

            if (admin == null || target == null || admin.AccountType != "admin")
                return false;

            // In a real implementation, you would create an impersonation token
            // For now, just validate that admin can impersonate
            return true;
        }

        public async Task<bool> BulkUpdateUsersAsync(Guid[] userIds, Action<User> updateAction)
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId) && !u.IsDeleted)
                .ToListAsync();

            foreach (var user in users)
            {
                updateAction(user);
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendBulkNotificationAsync(Guid[] userIds, string message)
        {
            // This would typically send notifications via email, push, etc.
            // For now, just validate the users exist
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId) && !u.IsDeleted && u.IsActive)
                .ToListAsync();

            return users.Count > 0;
        }

        public async Task<string> ExportUserDataAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return null;

            // Get related data
            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .ToListAsync();

            var appointments = await _context.Appointments
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .ToListAsync();

            // Create JSON export
            var exportData = new
            {
                user = new
                {
                    user.UserId,
                    user.Email,
                    user.Username,
                    user.DisplayName,
                    user.FirstName,
                    user.LastName,
                    user.AccountType,
                    user.CreatedAt,
                    user.LastLoginAt
                },
                statistics = new
                {
                    total_tasks = tasks.Count,
                    total_appointments = appointments.Count,
                    completed_tasks = tasks.Count(t => t.Status == "completed")
                },
                last_updated = DateTimeOffset.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(exportData);
        }

        public async Task<byte[]> ExportUsersToCsvAsync(IEnumerable<Guid> userIds = null)
        {
            var query = _context.Users.Where(u => !u.IsDeleted);

            if (userIds != null)
                query = query.Where(u => userIds.Contains(u.UserId));

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            // Create CSV content
            var csvLines = new List<string>
            {
                "UserId,Email,Username,DisplayName,AccountType,CreatedAt,LastLoginAt,IsActive"
            };

            foreach (var user in users)
            {
                csvLines.Add($"{user.UserId},{user.Email},{user.Username},{user.DisplayName},{user.AccountType},{user.CreatedAt:yyyy-MM-dd HH:mm:ss},{user.LastLoginAt:yyyy-MM-dd HH:mm:ss},{user.IsActive}");
            }

            return System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, csvLines));
        }

        public async Task<bool> EnableTwoFactorAsync(Guid userId, string twoFactorSecret)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            // In a real implementation, store the secret securely
            // user.TwoFactorSecret = twoFactorSecret;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DisableTwoFactorAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            // user.TwoFactorSecret = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateTwoFactorCodeAsync(Guid userId, string code)
        {
            // In a real implementation, validate the TOTP code
            // For now, return true for demonstration
            return await Task.FromResult(true);
        }
    }
}