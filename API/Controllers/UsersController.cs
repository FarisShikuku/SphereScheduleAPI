using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.Mappings;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SphereScheduleAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly PasswordService _passwordService;

        public UsersController(IUserService userService, IMapper mapper, PasswordService passwordService)
        {
            _userService = userService;
            _mapper = mapper;
            _passwordService = passwordService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }

        private bool IsAdminUser()
        {
            var accountTypeClaim = User.FindFirst("account_type")?.Value;
            return accountTypeClaim == "admin";
        }

        // GET: api/users/profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetCurrentUserProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(_mapper.Map<UserDto>(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user profile", error = ex.Message });
            }
        }

        // PUT: api/users/profile
        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateUserProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updateUserDto = _mapper.Map<UpdateUserDto>(updateDto);
                var updatedUser = await _userService.UpdateUserProfileAsync(userId, updateUserDto);

                return Ok(_mapper.Map<UserDto>(updatedUser));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user profile", error = ex.Message });
            }
        }

        // GET: api/users/preferences
        [HttpGet("preferences")]
        public async Task<ActionResult<object>> GetUserPreferences()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                try
                {
                    var preferences = System.Text.Json.JsonSerializer.Deserialize<object>(user.Preferences);
                    return Ok(preferences);
                }
                catch
                {
                    return Ok(new { message = "Invalid preferences format", raw = user.Preferences });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user preferences", error = ex.Message });
            }
        }

        // PUT: api/users/preferences
        [HttpPut("preferences")]
        public async Task<ActionResult> UpdateUserPreferences([FromBody] UpdateUserPreferencesDto preferencesDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.UpdateUserPreferencesAsync(userId, preferencesDto.PreferencesJson);

                if (!success)
                    return StatusCode(500, new { message = "Failed to update preferences" });

                return Ok(new { message = "Preferences updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user preferences", error = ex.Message });
            }
        }

        // POST: api/users/change-password
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangeUserPasswordDto passwordDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.ChangePasswordAsync(userId, passwordDto.CurrentPassword, passwordDto.NewPassword);

                if (!success)
                    return BadRequest(new { message = "Current password is incorrect" });

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing password", error = ex.Message });
            }
        }

        // POST: api/users/reset-password-request
        [AllowAnonymous]
        [HttpPost("reset-password-request")]
        public async Task<ActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestDto requestDto)
        {
            try
            {
                // In a real implementation, you would send an email with a reset token
                // For now, just validate the email exists
                var user = await _userService.GetUserByEmailAsync(requestDto.Email);

                if (user == null)
                {
                    // Don't reveal that the user doesn't exist for security reasons
                    return Ok(new { message = "If an account exists with this email, a reset link has been sent" });
                }

                // TODO: Generate reset token and send email
                return Ok(new { message = "If an account exists with this email, a reset link has been sent" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing password reset request", error = ex.Message });
            }
        }

        // POST: api/users/reset-password
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            try
            {
                // TODO: Validate reset token
                // For now, assume token validation is done elsewhere
                var success = await _userService.ResetPasswordAsync("user@example.com", resetDto.NewPassword);

                if (!success)
                    return BadRequest(new { message = "Invalid or expired reset token" });

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resetting password", error = ex.Message });
            }
        }

        // POST: api/users/verify-email
        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailDto verifyDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.VerifyEmailAsync(userId);

                if (!success)
                    return StatusCode(500, new { message = "Failed to verify email" });

                return Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error verifying email", error = ex.Message });
            }
        }

        // GET: api/users/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<UserDashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                var stats = await _userService.GetUserDashboardStatsAsync(userId);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving dashboard stats", error = ex.Message });
            }
        }

        // GET: api/users/activity
        [HttpGet("activity")]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivity(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var activities = await _userService.GetUserActivityLogsAsync(userId, startDate, endDate);

                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user activity", error = ex.Message });
            }
        }

        // POST: api/users/deactivate
        [HttpPost("deactivate")]
        public async Task<ActionResult> DeactivateAccount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.DeactivateAccountAsync(userId);

                if (!success)
                    return StatusCode(500, new { message = "Failed to deactivate account" });

                return Ok(new { message = "Account deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deactivating account", error = ex.Message });
            }
        }

        // POST: api/users/reactivate
        [HttpPost("reactivate")]
        public async Task<ActionResult> ReactivateAccount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.ReactivateAccountAsync(userId);

                if (!success)
                    return StatusCode(500, new { message = "Failed to reactivate account" });

                return Ok(new { message = "Account reactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reactivating account", error = ex.Message });
            }
        }

        // POST: api/users/upgrade
        [HttpPost("upgrade")]
        public async Task<ActionResult> UpgradeAccount([FromBody] UpgradeAccountDto upgradeDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.UpgradeAccountAsync(userId, upgradeDto.NewAccountType, upgradeDto.SubscriptionEndDate);

                if (!success)
                    return StatusCode(500, new { message = "Failed to upgrade account" });

                return Ok(new { message = "Account upgraded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error upgrading account", error = ex.Message });
            }
        }

        // POST: api/users/cancel-subscription
        [HttpPost("cancel-subscription")]
        public async Task<ActionResult> CancelSubscription()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.CancelSubscriptionAsync(userId);

                if (!success)
                    return StatusCode(500, new { message = "Failed to cancel subscription" });

                return Ok(new { message = "Subscription cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error cancelling subscription", error = ex.Message });
            }
        }

        // POST: api/users/enable-two-factor
        [HttpPost("enable-two-factor")]
        public async Task<ActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto twoFactorDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.EnableTwoFactorAsync(userId, twoFactorDto.TwoFactorCode);

                if (!success)
                    return StatusCode(500, new { message = "Failed to enable two-factor authentication" });

                return Ok(new { message = "Two-factor authentication enabled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error enabling two-factor authentication", error = ex.Message });
            }
        }

        // POST: api/users/disable-two-factor
        [HttpPost("disable-two-factor")]
        public async Task<ActionResult> DisableTwoFactor()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userService.DisableTwoFactorAsync(userId);

                if (!success)
                    return StatusCode(500, new { message = "Failed to disable two-factor authentication" });

                return Ok(new { message = "Two-factor authentication disabled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error disabling two-factor authentication", error = ex.Message });
            }
        }

        // GET: api/users/check-email
        [AllowAnonymous]
        [HttpGet("check-email")]
        public async Task<ActionResult> CheckEmailAvailability([FromQuery] string email, [FromQuery] Guid? excludeUserId = null)
        {
            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email, excludeUserId);
                return Ok(new { email, isAvailable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking email availability", error = ex.Message });
            }
        }

        // GET: api/users/check-username
        [AllowAnonymous]
        [HttpGet("check-username")]
        public async Task<ActionResult> CheckUsernameAvailability([FromQuery] string username, [FromQuery] Guid? excludeUserId = null)
        {
            try
            {
                var isAvailable = await _userService.IsUsernameAvailableAsync(username, excludeUserId);
                return Ok(new { username, isAvailable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking username availability", error = ex.Message });
            }
        }

        // GET: api/users/export-data
        [HttpGet("export-data")]
        public async Task<ActionResult> ExportUserData()
        {
            try
            {
                var userId = GetCurrentUserId();
                var exportData = await _userService.ExportUserDataAsync(userId);

                if (string.IsNullOrEmpty(exportData))
                    return StatusCode(500, new { message = "Failed to export user data" });

                var bytes = System.Text.Encoding.UTF8.GetBytes(exportData);
                return File(bytes, "application/json", $"user-data-{userId}.json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting user data", error = ex.Message });
            }
        }

        // ========== ADMIN ENDPOINTS ==========

        // GET: api/users (Admin only)
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeDeleted = false)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(page, pageSize, includeDeleted);
                return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        // GET: api/users/{id} (Admin only)
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return Ok(_mapper.Map<UserDto>(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
            }
        }

        // GET: api/users/statistics (Admin only)
        [HttpGet("statistics")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics()
        {
            try
            {
                var stats = await _userService.GetUserStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user statistics", error = ex.Message });
            }
        }

        // GET: api/users/search (Admin only)
        [HttpGet("search")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers(
            [FromQuery] string term,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var users = await _userService.SearchUsersAsync(term, page, pageSize);
                return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching users", error = ex.Message });
            }
        }

        // POST: api/users/{id}/lock (Admin only)
        [HttpPost("{id}/lock")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> LockUserAccount(Guid id, [FromBody] LockUserAccountDto lockDto)
        {
            try
            {
                var success = await _userService.LockUserAccountAsync(id, lockDto.LockoutEnd);
                if (!success)
                    return StatusCode(500, new { message = "Failed to lock user account" });

                return Ok(new { message = "User account locked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error locking user account", error = ex.Message });
            }
        }

        // POST: api/users/{id}/unlock (Admin only)
        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> UnlockUserAccount(Guid id)
        {
            try
            {
                var success = await _userService.UnlockUserAccountAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to unlock user account" });

                return Ok(new { message = "User account unlocked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error unlocking user account", error = ex.Message });
            }
        }

        // POST: api/users/{id}/role (Admin only)
        [HttpPost("{id}/role")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto roleDto)
        {
            try
            {
                var success = await _userService.UpdateUserRoleAsync(id, roleDto.NewRole);
                if (!success)
                    return StatusCode(500, new { message = "Failed to update user role" });

                return Ok(new { message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user role", error = ex.Message });
            }
        }

        // DELETE: api/users/{id} (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteUser(Guid id, [FromQuery] bool permanent = false)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id, permanent);
                if (!success)
                    return StatusCode(500, new { message = "Failed to delete user" });

                return Ok(new { message = $"User {(permanent ? "permanently " : "")}deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        // POST: api/users/{id}/restore (Admin only)
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> RestoreUser(Guid id)
        {
            try
            {
                var success = await _userService.RestoreUserAsync(id);
                if (!success)
                    return StatusCode(500, new { message = "Failed to restore user" });

                return Ok(new { message = "User restored successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error restoring user", error = ex.Message });
            }
        }

        // POST: api/users/bulk/notify (Admin only)
        [HttpPost("bulk/notify")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> SendBulkNotification([FromBody] BulkNotificationDto notificationDto)
        {
            try
            {
                var success = await _userService.SendBulkNotificationAsync(notificationDto.UserIds, notificationDto.Message);
                if (!success)
                    return StatusCode(500, new { message = "Failed to send notifications" });

                return Ok(new { message = "Notifications sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notifications", error = ex.Message });
            }
        }

        // POST: api/users/export (Admin only)
        [HttpPost("export")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> ExportUsers([FromBody] ExportUsersDto exportDto)
        {
            try
            {
                byte[] exportData;

                if (exportDto.Format?.ToLower() == "json")
                {
                    // JSON export
                    var users = await _userService.GetAllUsersAsync(1, int.MaxValue);
                    exportData = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(users));
                    return File(exportData, "application/json", $"users-export-{DateTime.UtcNow:yyyyMMdd}.json");
                }
                else
                {
                    // CSV export (default)
                    exportData = await _userService.ExportUsersToCsvAsync(exportDto.UserIds);
                    return File(exportData, "text/csv", $"users-export-{DateTime.UtcNow:yyyyMMdd}.csv");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting users", error = ex.Message });
            }
        }
    }

    // Supporting DTO classes for this controller
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
}