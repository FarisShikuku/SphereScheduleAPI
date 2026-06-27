// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║  AuthService.cs — CHANGE LOG                                                 ║
// ║                                                                              ║
// ║  FIX 1 — RegisterAsync: email send wrapped in nested try-catch.              ║
// ║    Root cause: _emailService.SendEmailAsync() threw (no SMTP configured),    ║
// ║    which bubbled up through the outer catch and returned the generic          ║
// ║    "An error occurred during registration" even though the user record        ║
// ║    was already saved. Email is non-critical at registration time.             ║
// ║                                                                              ║
// ║  FIX 2 — RegisterAsync: username collision guard added.                      ║
// ║    Root cause: username was derived from email prefix (e.g. "john" from      ║
// ║    "john@gmail.com"). The Users table has a unique index on Username with     ║
// ║    filter [IsDeleted]=0. Two users with the same prefix (john@gmail.com and  ║
// ║    john@yahoo.com) would cause a DB unique constraint violation on the        ║
// ║    second registration. Fix: append a short unique suffix when the derived   ║
// ║    username is already taken.                                                 ║
// ║                                                                              ║
// ║  FIX 3 — RegisterAsync: prevent NULL values in non-nullable string columns   ║
// ║    Root cause: PhoneNumber, AvatarUrl, GoogleId, MicrosoftId, FacebookId     ║
// ║    were left NULL when not provided, but the User entity has these as         ║
// ║    non-nullable strings (string without '?'). This caused                    ║
// ║    SqlNullValueException during login when EF Core tried to map the          ║
// ║    database NULL to a non-nullable C# property.                              ║
// ║    Fix: set default values to string.Empty when input is null/missing.       ║
// ╚══════════════════════════════════════════════════════════════════════════════╝

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Domain.Entities;
using SphereScheduleAPI.Infrastructure.Data;
using SphereScheduleAPI.Infrastructure.Services;

namespace SphereScheduleAPI.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtService _jwtService;
        private readonly PasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IActivityLogService _activityLogService;

        public AuthService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<AuthService> logger,
            JwtService jwtService,
            PasswordService passwordService,
            IEmailService emailService,
            IConfiguration configuration,
            IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _emailService = emailService;
            _configuration = configuration;
            _activityLogService = activityLogService;
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────────
        public async Task<AuthResponseByTokenDto> LoginAsync(AuthLoginDto loginDto, string ipAddress, string userAgent)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found with email {Email}", loginDto.Email);
                    await _activityLogService.LogUserLoginAsync(Guid.Empty, ipAddress, userAgent, false, "User not found");
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt) ||
                    !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                {
                    user.AccessFailedCount++;
                    if (user.LockoutEnabled && user.AccessFailedCount >= 5)
                    {
                        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Login failed: Invalid password for user {Email}", loginDto.Email);
                    await _activityLogService.LogUserLoginAsync(user.UserID, ipAddress, userAgent, false, "Invalid password");
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Login failed: Account locked for user {Email}", loginDto.Email);
                    throw new UnauthorizedAccessException($"Account is locked until {user.LockoutEnd.Value:g}");
                }

                user.AccessFailedCount = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTimeOffset.UtcNow;
                user.LastActivityAt = DateTimeOffset.UtcNow;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                var roles = GetUserRoles(user.AccountType);
                var token = _jwtService.GenerateToken(user.UserID, user.Email, user.Username ?? user.Email, roles);

                var response = new AuthResponseByTokenDto
                {
                    Token = token,
                    ExpiresAt = _jwtService.GetTokenExpiration(token),
                    User = _mapper.Map<UserDto>(user),
                    Roles = roles,
                    TokenType = "Bearer"
                };

                await _activityLogService.LogUserLoginAsync(user.UserID, ipAddress, userAgent, true, "Login successful");
                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", loginDto.Email);
                throw;
            }
        }

        // ── REGISTER ──────────────────────────────────────────────────────────────
        public async Task<AuthResponseByTokenDto> RegisterAsync(AuthRegisterDto registerDto)
        {
            try
            {
                // Check if email is already taken
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Validate password complexity
                if (!_passwordService.IsPasswordStrong(registerDto.Password))
                {
                    throw new ArgumentException(
                        "Password must be at least 8 characters with uppercase, lowercase, number, and special character");
                }

                // Hash password using PBKDF2
                var (hash, salt) = _passwordService.HashPassword(registerDto.Password);

                // ── [FIX 2] Username collision guard ──────────────────────────────
                var baseUsername = registerDto.Email.Split('@')[0]
                    .ToLower()
                    .Replace(".", "")
                    .Replace("+", "");

                var username = baseUsername;
                var attempt = 0;
                while (await _context.Users.AnyAsync(u => u.Username == username && !u.IsDeleted))
                {
                    attempt++;
                    username = $"{baseUsername}{attempt + 1}";
                }
                // ── End of username fix ───────────────────────────────────────────

                // ── [FIX 3] Prevent NULL values in non-nullable string columns ─────
                // The User entity has non-nullable string properties (no '?').
                // If these are left NULL, EF Core throws SqlNullValueException.
                // Solution: set default values to string.Empty when input is null.
                var user = new User
                {
                    UserID = Guid.NewGuid(),
                    Email = registerDto.Email,
                    Username = username,
                    DisplayName = registerDto.DisplayName,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber ?? string.Empty,
                    DateOfBirth = registerDto.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    AvatarUrl = string.Empty,
                    GoogleId = string.Empty,
                    MicrosoftId = string.Empty,
                    FacebookId = string.Empty,
                    EmailVerified = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    AccountType = "free",
                    SubscriptionStartDate = null,
                    SubscriptionEndDate = null,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = null,
                    LastActivityAt = null,
                    DeletedAt = null,
                    Preferences = @"{
                        ""theme"": ""light"",
                        ""timezone"": ""UTC"",
                        ""language"": ""en"",
                        ""notificationSettings"": {
                            ""email"": true,
                            ""push"": true,
                            ""sms"": false
                        },
                        ""workHours"": {
                            ""start"": ""09:00"",
                            ""end"": ""17:00""
                        },
                        ""weekStartDay"": 1
                    }"
                };
                // ── End of NULL prevention fix ─────────────────────────────────────

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create default task categories for the new user
                await CreateDefaultCategoriesAsync(user.UserID);

                // ── [FIX 1] Email send is now non-blocking ────────────────────────
                try
                {
                    var verificationToken = Guid.NewGuid().ToString();
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Welcome to Sphere Schedule - Verify Your Email",
                        $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                        $"Thank you for registering with Sphere Schedule!<br><br>" +
                        $"Your verification token: {verificationToken}<br><br>" +
                        $"Best regards,<br>Sphere Schedule Team"
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx,
                        "Failed to send welcome email to {Email}. " +
                        "Registration succeeded. Configure SMTP in appsettings to enable email.",
                        user.Email);
                }
                // ── End of email fix ──────────────────────────────────────────────

                var roles = GetUserRoles(user.AccountType);
                var token = _jwtService.GenerateToken(user.UserID, user.Email, user.Username ?? user.Email, roles);

                var response = new AuthResponseByTokenDto
                {
                    Token = token,
                    ExpiresAt = _jwtService.GetTokenExpiration(token),
                    User = _mapper.Map<UserDto>(user),
                    Roles = roles,
                    TokenType = "Bearer"
                };

                await _activityLogService.LogEntityCreatedAsync("user", user.UserID, user.UserID, "User registered");
                _logger.LogInformation("New user registered: {Email} with username: {Username}", registerDto.Email, username);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email {Email}", registerDto.Email);
                throw;
            }
        }

        // ── REFRESH TOKEN ─────────────────────────────────────────────────────────
        public async Task<AuthResponseByTokenDto> RefreshTokenAsync(AuthRefreshTokenDto refreshTokenDto)
        {
            try
            {
                _logger.LogWarning("Refresh token functionality not fully implemented");
                throw new NotImplementedException("Refresh token implementation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }

        // ── LOGOUT ────────────────────────────────────────────────────────────────
        public async Task<bool> LogoutAsync(Guid UserID, string token)
        {
            try
            {
                var user = await _context.Users.FindAsync(UserID);
                if (user != null)
                {
                    user.LastActivityAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await _activityLogService.LogUserLogoutAsync(UserID, "N/A", "N/A");
                _logger.LogInformation("User {UserID} logged out", UserID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserID}", UserID);
                return false;
            }
        }

        // ── FORGOT PASSWORD ───────────────────────────────────────────────────────
        public async Task<bool> ForgotPasswordAsync(AuthForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", forgotPasswordDto.Email);
                    return true;
                }

                var resetToken = Guid.NewGuid().ToString();
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
                var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Sphere Schedule - Password Reset Request",
                    $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                    $"You requested to reset your password. Click the link below:<br><br>" +
                    $"<a href='{resetLink}'>{resetLink}</a><br><br>" +
                    $"This link will expire in 1 hour.<br><br>" +
                    $"If you didn't request this, please ignore this email.<br><br>" +
                    $"Best regards,<br>Sphere Schedule Team"
                );

                _logger.LogInformation("Password reset email sent to {Email}", forgotPasswordDto.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", forgotPasswordDto.Email);
                throw;
            }
        }

        // ── RESET PASSWORD ────────────────────────────────────────────────────────
        public async Task<bool> ResetPasswordAsync(AuthResetPasswordDto resetPasswordDto)
        {
            try
            {
                _logger.LogWarning("Password reset functionality not fully implemented");
                throw new NotImplementedException("Password reset token validation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                throw;
            }
        }

        // ── CHANGE PASSWORD ───────────────────────────────────────────────────────
        public async Task<bool> ChangePasswordAsync(Guid UserID, AuthChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(UserID);
                if (user == null)
                    throw new KeyNotFoundException("User not found");

                if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                    throw new UnauthorizedAccessException("Current password is incorrect");

                if (!_passwordService.IsPasswordStrong(changePasswordDto.NewPassword))
                    throw new ArgumentException(
                        "New password must be at least 8 characters with uppercase, lowercase, number, and special character");

                var (hash, salt) = _passwordService.HashPassword(changePasswordDto.NewPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Sphere Schedule - Password Changed",
                    $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                    $"Your password has been successfully changed.<br><br>" +
                    $"If you didn't make this change, please contact support immediately.<br><br>" +
                    $"Best regards,<br>Sphere Schedule Team"
                );

                _logger.LogInformation("Password changed for user {UserID}", UserID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserID}", UserID);
                throw;
            }
        }

        // ── VERIFY EMAIL ──────────────────────────────────────────────────────────
        public async Task<bool> VerifyEmailAsync(AuthVerifyEmailDto verifyEmailDto)
        {
            try
            {
                _logger.LogWarning("Email verification not fully implemented");
                throw new NotImplementedException("Email verification token validation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email with token");
                throw;
            }
        }

        // ── RESEND VERIFICATION ───────────────────────────────────────────────────
        public async Task<bool> ResendVerificationEmailAsync(AuthResendVerificationDto resendVerificationDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == resendVerificationDto.Email && !u.EmailVerified && u.IsActive);

                if (user == null) return true;

                var verificationToken = Guid.NewGuid().ToString();
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Sphere Schedule - Verify Your Email",
                    $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                    $"Please verify your email by clicking the link below:<br><br>" +
                    $"<a href='https://localhost:5001/verify-email?token={verificationToken}'>Verify Email</a><br><br>" +
                    $"If you didn't create an account, please ignore this email.<br><br>" +
                    $"Best regards,<br>Sphere Schedule Team"
                );

                _logger.LogInformation("Verification email resent to {Email}", resendVerificationDto.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email to {Email}", resendVerificationDto.Email);
                throw;
            }
        }

        // ── VALIDATE TOKEN ────────────────────────────────────────────────────────
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);
                return principal != null && !_jwtService.IsTokenExpired(token);
            }
            catch
            {
                return false;
            }
        }

        // ── GET CURRENT USER ──────────────────────────────────────────────────────
        public async Task<UserDto> GetCurrentUserAsync(string token)
        {
            var UserID = _jwtService.GetUserIDFromToken(token);
            if (!UserID.HasValue)
                throw new UnauthorizedAccessException("Invalid token");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == UserID.Value && u.IsActive && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            return _mapper.Map<UserDto>(user);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────────

        private List<string> GetUserRoles(string accountType)
        {
            return accountType switch
            {
                "admin" => new List<string> { "user", "admin" },
                "premium" => new List<string> { "user", "premium" },
                "enterprise" => new List<string> { "user", "enterprise" },
                _ => new List<string> { "user" }
            };
        }

        private async Task CreateDefaultCategoriesAsync(Guid UserID)
        {
            var defaultCategories = new[]
            {
                new Category { CategoryID = Guid.NewGuid(), UserID = UserID, CategoryName = "Work",      CategoryType = "system", ColorCode = "#2196F3", IconName = "work",   IsDefault = true, CategoryOrder = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Category { CategoryID = Guid.NewGuid(), UserID = UserID, CategoryName = "Personal",  CategoryType = "system", ColorCode = "#4CAF50", IconName = "person", IsDefault = true, CategoryOrder = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Category { CategoryID = Guid.NewGuid(), UserID = UserID, CategoryName = "Health",    CategoryType = "system", ColorCode = "#F44336", IconName = "health", IsDefault = true, CategoryOrder = 3, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Category { CategoryID = Guid.NewGuid(), UserID = UserID, CategoryName = "Education", CategoryType = "system", ColorCode = "#9C27B0", IconName = "school", IsDefault = true, CategoryOrder = 4, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            };

            _context.Categories.AddRange(defaultCategories);
            await _context.SaveChangesAsync();
        }
    }
}