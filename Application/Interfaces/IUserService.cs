using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IUserService
    {
        // Basic CRUD
        Task<User> GetUserByIdAsync(Guid userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync(int page = 1, int pageSize = 20, bool includeDeleted = false);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid userId, bool permanent = false);
        Task<bool> RestoreUserAsync(Guid userId);

        // Authentication & Security
        Task<bool> ValidateUserCredentialsAsync(string emailOrUsername, string password);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<bool> VerifyEmailAsync(Guid userId);
        Task<bool> UpdateLastLoginAsync(Guid userId);
        Task<bool> UpdateLastActivityAsync(Guid userId);
        Task<bool> LockUserAccountAsync(Guid userId, DateTimeOffset? lockoutEnd = null);
        Task<bool> UnlockUserAccountAsync(Guid userId);

        // Profile Management
        Task<User> UpdateUserProfileAsync(Guid userId, UpdateUserDto updateDto);
        Task<bool> UpdateUserPreferencesAsync(Guid userId, string preferencesJson);
        Task<bool> UpdateAvatarAsync(Guid userId, string avatarUrl);

        // Account Management
        Task<bool> DeactivateAccountAsync(Guid userId);
        Task<bool> ReactivateAccountAsync(Guid userId);
        Task<bool> UpgradeAccountAsync(Guid userId, string newAccountType, DateTime? subscriptionEndDate = null);
        Task<bool> CancelSubscriptionAsync(Guid userId);

        // Validation & Checks
        Task<bool> UserExistsByEmailAsync(string email);
        Task<bool> UserExistsByUsernameAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);
        Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserId = null);

        // Statistics & Analytics
        Task<int> GetTotalUsersCountAsync(bool includeDeleted = false);
        Task<Dictionary<string, int>> GetUserStatisticsAsync();
        Task<IEnumerable<UserActivityDto>> GetUserActivityLogsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> GetUserDashboardStatsAsync(Guid userId);

        // Search & Filter
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetUsersByAccountTypeAsync(string accountType, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetUsersByStatusAsync(bool isActive, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetInactiveUsersAsync(int daysInactive, int page = 1, int pageSize = 20);

        // Admin Operations
        Task<bool> UpdateUserRoleAsync(Guid userId, string accountType);
        Task<bool> ImpersonateUserAsync(Guid adminUserId, Guid targetUserId);
        Task<bool> BulkUpdateUsersAsync(Guid[] userIds, Action<User> updateAction);
        Task<bool> SendBulkNotificationAsync(Guid[] userIds, string message);

        // Data Export
        Task<string> ExportUserDataAsync(Guid userId);
        Task<byte[]> ExportUsersToCsvAsync(IEnumerable<Guid> userIds = null);

        // Two-Factor Authentication
        Task<bool> EnableTwoFactorAsync(Guid userId, string twoFactorSecret);
        Task<bool> DisableTwoFactorAsync(Guid userId);
        Task<bool> ValidateTwoFactorCodeAsync(Guid userId, string code);
    }
}