using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    public class AuthLoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // ── [FIX] Removed [MinLength(8)] ──────────────────────────────────────
        // Login must never validate password complexity. The DTO just checks the
        // field is present — the service verifies it against the stored hash.
        // MinLength was causing 400s before the service was even reached.
        [Required(ErrorMessage = "Password is required")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class AuthRegisterDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Complexity enforced by PasswordService.IsPasswordStrong() in AuthService.
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required")]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        // ── [FIX] Removed [Phone] attribute ───────────────────────────────────
        // [Phone] validates empty strings and rejects them even when the field is
        // nullable, producing 400 errors when the frontend omits phoneNumber.
        // The DB column has no NOT NULL constraint — it is genuinely optional.
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }

    public class AuthResponseByTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public string TokenType { get; set; } = "Bearer";
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }

    public class AuthRefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }

    public class AuthResetPasswordDto
    {
        [Required(ErrorMessage = "Reset token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthVerifyEmailDto
    {
        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }

    public class AuthResendVerificationDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }
}