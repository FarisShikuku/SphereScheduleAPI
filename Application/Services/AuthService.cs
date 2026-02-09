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

        public async Task<AuthResponseByTokenDto> LoginAsync(AuthLoginDto loginDto, string ipAddress, string userAgent)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found with email {Email}", loginDto.Email);
                    await _activityLogService.LogUserLoginAsync(Guid.Empty, ipAddress, userAgent, false, "User not found");
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                // Verify password
                if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt) ||
                    !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                {
                    // Increment failed login attempts
                    user.AccessFailedCount++;
                    if (user.LockoutEnabled && user.AccessFailedCount >= 5)
                    {
                        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                    }
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Login failed: Invalid password for user {Email}", loginDto.Email);
                    await _activityLogService.LogUserLoginAsync(user.UserId, ipAddress, userAgent, false, "Invalid password");
                    throw new UnauthorizedAccessException("Invalid credentials");
                }

                // Check if account is locked
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Login failed: Account locked for user {Email}", loginDto.Email);
                    throw new UnauthorizedAccessException($"Account is locked until {user.LockoutEnd.Value:g}");
                }

                // Reset failed login attempts
                user.AccessFailedCount = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTimeOffset.UtcNow;
                user.LastActivityAt = DateTimeOffset.UtcNow;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                // Generate JWT token
                var roles = GetUserRoles(user.AccountType);
                var token = _jwtService.GenerateToken(user.UserId, user.Email, user.Username ?? user.Email, roles);

                // Create response
                var response = new AuthResponseByTokenDto
                {
                    Token = token,
                    ExpiresAt = _jwtService.GetTokenExpiration(token),
                    User = _mapper.Map<UserDto>(user),
                    Roles = roles,
                    TokenType = "Bearer"
                };

                // Log successful login
                await _activityLogService.LogUserLoginAsync(user.UserId, ipAddress, userAgent, true, "Login successful");

                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", loginDto.Email);
                throw;
            }
        }

        public async Task<AuthResponseByTokenDto> RegisterAsync(AuthRegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Validate password strength
                if (!_passwordService.IsPasswordStrong(registerDto.Password))
                {
                    throw new ArgumentException("Password must be at least 8 characters with uppercase, lowercase, number, and special character");
                }

                // Hash password
                var (hash, salt) = _passwordService.HashPassword(registerDto.Password);

                // Create new user
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = registerDto.Email,
                    Username = registerDto.Email.Split('@')[0], // Default username from email
                    DisplayName = registerDto.DisplayName,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    DateOfBirth = registerDto.DateOfBirth?.ToDateTime(TimeOnly.MinValue), // FIXED: Convert DateOnly? to DateTime?
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    EmailVerified = false, // Email verification required
                    AccountType = "free", // Default account type
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Preferences = @"{
                        ""theme"": ""light"",
                        ""timezone"": ""UTC"",
                        ""language"": ""en"",
                        ""notificationSettings"": {
                            ""email"": true,
                            ""push"": true,
                            ""sms"": false
                        }
                    }"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create default categories for new user
                await CreateDefaultCategoriesAsync(user.UserId);

                // Generate verification token (simplified)
                var verificationToken = Guid.NewGuid().ToString();

                // Send verification email
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Welcome to Sphere Schedule - Verify Your Email",
                    $"Please verify your email by clicking this link: ..."
                );

                // Generate JWT token
                var roles = GetUserRoles(user.AccountType);
                var token = _jwtService.GenerateToken(user.UserId, user.Email, user.Username ?? user.Email, roles);

                // Create response
                var response = new AuthResponseByTokenDto
                {
                    Token = token,
                    ExpiresAt = _jwtService.GetTokenExpiration(token),
                    User = _mapper.Map<UserDto>(user),
                    Roles = roles,
                    TokenType = "Bearer"
                };

                // Log registration
                await _activityLogService.LogEntityCreatedAsync("user", user.UserId, user.UserId, "User registered");

                _logger.LogInformation("New user registered: {Email}", registerDto.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email {Email}", registerDto.Email);
                throw;
            }
        }

        public async Task<AuthResponseByTokenDto> RefreshTokenAsync(AuthRefreshTokenDto refreshTokenDto)
        {
            try
            {
                // In a real implementation:
                // 1. Validate the refresh token against database
                // 2. Check if it's not revoked
                // 3. Check expiration
                // 4. Generate new access token
                // 5. Optionally rotate refresh token

                _logger.LogWarning("Refresh token functionality not fully implemented");
                throw new NotImplementedException("Refresh token implementation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(Guid userId, string token)
        {
            try
            {
                // In a real implementation, you might:
                // 1. Add token to blacklist
                // 2. Update user's last logout time
                // 3. Log the activity

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastActivityAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Log logout activity
                await _activityLogService.LogUserLogoutAsync(userId, "N/A", "N/A");

                _logger.LogInformation("User {UserId} logged out", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ForgotPasswordAsync(AuthForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    // Don't reveal that user doesn't exist (security best practice)
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", forgotPasswordDto.Email);
                    return true;
                }

                // Generate reset token (in real implementation, use proper token service)
                var resetToken = Guid.NewGuid().ToString();
                var resetTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);

                // Store reset token in database (simplified - add columns to User entity)
                // user.ResetPasswordToken = resetToken;
                // user.ResetPasswordTokenExpiry = resetTokenExpiry;
                // await _context.SaveChangesAsync();

                // Send reset email
                var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
                var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Sphere Schedule - Password Reset Request",
                    $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                    $"You requested to reset your password. Click the link below to reset it:<br><br>" +
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

        public async Task<bool> ResetPasswordAsync(AuthResetPasswordDto resetPasswordDto)
        {
            try
            {
                // In real implementation:
                // 1. Validate token from database
                // 2. Check expiration
                // 3. Find user by token
                // 4. Hash new password and update user
                // 5. Invalidate token

                _logger.LogWarning("Password reset functionality not fully implemented");
                throw new NotImplementedException("Password reset token validation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, AuthChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found");
                }

                // Verify current password
                if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    throw new UnauthorizedAccessException("Current password is incorrect");
                }

                // Validate new password strength
                if (!_passwordService.IsPasswordStrong(changePasswordDto.NewPassword))
                {
                    throw new ArgumentException("New password must be at least 8 characters with uppercase, lowercase, number, and special character");
                }

                // Hash new password
                var (hash, salt) = _passwordService.HashPassword(changePasswordDto.NewPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification email
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Sphere Schedule - Password Changed",
                    $"Hello {user.DisplayName ?? user.Email},<br><br>" +
                    $"Your password has been successfully changed.<br><br>" +
                    $"If you didn't make this change, please contact support immediately.<br><br>" +
                    $"Best regards,<br>Sphere Schedule Team"
                );

                _logger.LogInformation("Password changed for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifyEmailAsync(AuthVerifyEmailDto verifyEmailDto)
        {
            try
            {
                // In real implementation:
                // 1. Validate verification token from database
                // 2. Find user by token
                // 3. Mark email as verified
                // 4. Invalidate token

                _logger.LogWarning("Email verification functionality not fully implemented");
                throw new NotImplementedException("Email verification token validation required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email with token");
                throw;
            }
        }

        public async Task<bool> ResendVerificationEmailAsync(AuthResendVerificationDto resendVerificationDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == resendVerificationDto.Email && !u.EmailVerified && u.IsActive);

                if (user == null)
                {
                    // Don't reveal if user exists
                    return true;
                }

                // Generate new verification token and send email
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

        public async Task<UserDto> GetCurrentUserAsync(string token)
        {
            var userId = _jwtService.GetUserIdFromToken(token);
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId.Value && u.IsActive && !u.IsDeleted);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return _mapper.Map<UserDto>(user);
        }

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

        private async Task CreateDefaultCategoriesAsync(Guid userId)
        {
            var defaultCategories = new[]
            {
                new Category
                {
                    CategoryId = Guid.NewGuid(),
                    UserId = userId,
                    CategoryName = "Work",
                    CategoryType = "system",
                    ColorCode = "#2196F3",
                    IconName = "work",
                    IsDefault = true,
                    CategoryOrder = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Category
                {
                    CategoryId = Guid.NewGuid(),
                    UserId = userId,
                    CategoryName = "Personal",
                    CategoryType = "system",
                    ColorCode = "#4CAF50",
                    IconName = "person",
                    IsDefault = true,
                    CategoryOrder = 2,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Category
                {
                    CategoryId = Guid.NewGuid(),
                    UserId = userId,
                    CategoryName = "Health",
                    CategoryType = "system",
                    ColorCode = "#F44336",
                    IconName = "health",
                    IsDefault = true,
                    CategoryOrder = 3,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Category
                {
                    CategoryId = Guid.NewGuid(),
                    UserId = userId,
                    CategoryName = "Education",
                    CategoryType = "system",
                    ColorCode = "#9C27B0",
                    IconName = "school",
                    IsDefault = true,
                    CategoryOrder = 4,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            _context.Categories.AddRange(defaultCategories);
            await _context.SaveChangesAsync();
        }

        // Helper method to get user email (used in notification summary)
        private async Task<string?> GetUserEmailAsync(Guid userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
        }
    }
}