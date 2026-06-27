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
        Task<User> GetUserByIdAsync(Guid UserID);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync(int page = 1, int pageSize = 20, bool includeDeleted = false);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid UserID, bool permanent = false);
        Task<bool> RestoreUserAsync(Guid UserID);

        // Authentication & Security
        Task<bool> ValidateUserCredentialsAsync(string emailOrUsername, string password);
        Task<bool> ChangePasswordAsync(Guid UserID, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<bool> VerifyEmailAsync(Guid UserID);
        Task<bool> UpdateLastLoginAsync(Guid UserID);
        Task<bool> UpdateLastActivityAsync(Guid UserID);
        Task<bool> LockUserAccountAsync(Guid UserID, DateTimeOffset? lockoutEnd = null);
        Task<bool> UnlockUserAccountAsync(Guid UserID);

        // Profile Management
        Task<User> UpdateUserProfileAsync(Guid UserID, UpdateUserDto updateDto);
        Task<bool> UpdateUserPreferencesAsync(Guid UserID, string preferencesJson);
        Task<bool> UpdateAvatarAsync(Guid UserID, string avatarUrl);

        // Account Management
        Task<bool> DeactivateAccountAsync(Guid UserID);
        Task<bool> ReactivateAccountAsync(Guid UserID);
        Task<bool> UpgradeAccountAsync(Guid UserID, string newAccountType, DateTime? subscriptionEndDate = null);
        Task<bool> CancelSubscriptionAsync(Guid UserID);

        // Validation & Checks
        Task<bool> UserExistsByEmailAsync(string email);
        Task<bool> UserExistsByUsernameAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserID = null);
        Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserID = null);

        // Statistics & Analytics
        Task<int> GetTotalUsersCountAsync(bool includeDeleted = false);
        Task<Dictionary<string, int>> GetUserStatisticsAsync();
        Task<IEnumerable<UserActivityDto>> GetUserActivityLogsAsync(Guid UserID, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, object>> GetUserDashboardStatsAsync(Guid UserID);

        // Search & Filter
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetUsersByAccountTypeAsync(string accountType, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetUsersByStatusAsync(bool isActive, int page = 1, int pageSize = 20);
        Task<IEnumerable<User>> GetInactiveUsersAsync(int daysInactive, int page = 1, int pageSize = 20);

        // Admin Operations
        Task<bool> UpdateUserRoleAsync(Guid UserID, string accountType);
        Task<bool> ImpersonateUserAsync(Guid adminUserID, Guid targetUserID);
        Task<bool> BulkUpdateUsersAsync(Guid[] UserIDs, Action<User> updateAction);
        Task<bool> SendBulkNotificationAsync(Guid[] UserIDs, string message);

        // Data Export
        Task<string> ExportUserDataAsync(Guid UserID);
        Task<byte[]> ExportUsersToCsvAsync(IEnumerable<Guid> UserIDs = null);

        // Two-Factor Authentication
        Task<bool> EnableTwoFactorAsync(Guid UserID, string twoFactorSecret);
        Task<bool> DisableTwoFactorAsync(Guid UserID);
        Task<bool> ValidateTwoFactorCodeAsync(Guid UserID, string code);
    }
}