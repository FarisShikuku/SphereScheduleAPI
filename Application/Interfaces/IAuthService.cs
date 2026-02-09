using SphereScheduleAPI.Application.DTOs;

namespace SphereScheduleAPI.Application.Interfaces
{
    public interface IAuthService
    {
        // Changed from LoginDto to AuthLoginDto
        Task<AuthResponseByTokenDto> LoginAsync(AuthLoginDto loginDto, string ipAddress, string userAgent);

        // Changed from RegisterDto to AuthRegisterDto
        Task<AuthResponseByTokenDto> RegisterAsync(AuthRegisterDto registerDto);

        // Changed from RefreshTokenDto to AuthRefreshTokenDto
        Task<AuthResponseByTokenDto> RefreshTokenAsync(AuthRefreshTokenDto refreshTokenDto);

        Task<bool> LogoutAsync(Guid userId, string token);

        // Changed from ForgotPasswordDto to AuthForgotPasswordDto
        Task<bool> ForgotPasswordAsync(AuthForgotPasswordDto forgotPasswordDto);

        // Changed from ResetPasswordDto to AuthResetPasswordDto
        Task<bool> ResetPasswordAsync(AuthResetPasswordDto resetPasswordDto);

        // Changed from ChangePasswordDto to AuthChangePasswordDto
        Task<bool> ChangePasswordAsync(Guid userId, AuthChangePasswordDto changePasswordDto);

        // Changed from VerifyEmailDto to AuthVerifyEmailDto
        Task<bool> VerifyEmailAsync(AuthVerifyEmailDto verifyEmailDto);

        // Changed from ResendVerificationDto to AuthResendVerificationDto
        Task<bool> ResendVerificationEmailAsync(AuthResendVerificationDto resendVerificationDto);

        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto> GetCurrentUserAsync(string token);
    }
}