using System;
using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string AccountType { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }
        public string Preferences { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public string AccountType { get; set; } = "free";
    }

    public class UpdateUserDto
    {
        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Url]
        [StringLength(500)]
        public string AvatarUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Url]
        [StringLength(500)]
        public string AvatarUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
    }

    public class UpdateUserPreferencesDto
    {
        [Required]
        public string PreferencesJson { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; }
    }

    public class ChangeUserPasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }

    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; }
    }

    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int FreeUsers { get; set; }
        public int PremiumUsers { get; set; }
        public int EnterpriseUsers { get; set; }
        public int AdminUsers { get; set; }
        public int EmailVerifiedUsers { get; set; }
        public int TwoFactorEnabledUsers { get; set; }
        public int LockedUsers { get; set; }
    }

    public class UserDashboardStatsDto
    {
        public UserInfoDto UserInfo { get; set; }
        public CountsDto Counts { get; set; }
        public ActivityDto Activity { get; set; }
    }

    public class UserInfoDto
    {
        public string DisplayName { get; set; }
        public string AccountType { get; set; }
        public bool EmailVerified { get; set; }
        public string MemberSince { get; set; }
    }

    public class CountsDto
    {
        public int TotalTasks { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedTasks { get; set; }
        public int UpcomingAppointments { get; set; }
    }

    public class ActivityDto
    {
        public string LastLogin { get; set; }
        public string LastActivity { get; set; }
        public int DaysActive { get; set; }
    }

    public class UserActivityDto
    {
        public string ActivityType { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Details { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class EnableTwoFactorDto
    {
        [Required]
        public string TwoFactorCode { get; set; }
    }

    public class ValidateTwoFactorDto
    {
        [Required]
        public string Code { get; set; }
    }

    public class UpgradeAccountDto
    {
        [Required]
        public string NewAccountType { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }

    public class LockUserAccountDto
    {
        public DateTimeOffset? LockoutEnd { get; set; }
    }

    public class UpdateUserRoleDto
    {
        [Required]
        public string NewRole { get; set; }
    }

    public class BulkUserActionDto
    {
        [Required]
        public Guid[] UserIds { get; set; }
    }

    public class BulkNotificationDto : BulkUserActionDto
    {
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
    }

    public class ExportUsersDto
    {
        public Guid[] UserIds { get; set; }
        public string Format { get; set; } = "csv";
    }

    public class CheckAvailabilityDto
    {
        public string Value { get; set; }
        public Guid? ExcludeUserId { get; set; }
    }
}