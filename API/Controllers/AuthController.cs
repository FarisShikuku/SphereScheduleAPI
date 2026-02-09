using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SphereScheduleAPI.Application.DTOs;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseByTokenDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] AuthLoginDto loginDto)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _authService.LoginAsync(loginDto, ipAddress, userAgent);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", loginDto.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
                return BadRequest(new { message = "An error occurred during login" });
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseByTokenDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] AuthRegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(Login), result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Password"))
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", registerDto.Email);
                return BadRequest(new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponseByTokenDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RefreshToken([FromBody] AuthRefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return BadRequest(new { message = "An error occurred refreshing token" });
            }
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ForgotPassword([FromBody] AuthForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(forgotPasswordDto);
                return Ok(new { message = "If an account exists, a password reset email has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", forgotPasswordDto.Email);
                return BadRequest(new { message = "An error occurred processing your request" });
            }
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] AuthResetPasswordDto resetPasswordDto)
        {
            try
            {
                var success = await _authService.ResetPasswordAsync(resetPasswordDto);
                if (success)
                {
                    return Ok(new { message = "Password has been reset successfully" });
                }
                return BadRequest(new { message = "Invalid or expired reset token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return BadRequest(new { message = "An error occurred resetting password" });
            }
        }

        [HttpPost("change-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ChangePassword([FromBody] AuthChangePasswordDto changePasswordDto)
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "No token provided" });
                }

                var userId = GetUserIdFromToken(token);
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var success = await _authService.ChangePasswordAsync(userId.Value, changePasswordDto);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return BadRequest(new { message = "An error occurred changing password" });
            }
        }

        [HttpGet("verify-email")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                var verifyDto = new AuthVerifyEmailDto { Token = token };
                var success = await _authService.VerifyEmailAsync(verifyDto);
                if (success)
                {
                    return Ok(new { message = "Email verified successfully" });
                }
                return BadRequest(new { message = "Invalid or expired verification token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return BadRequest(new { message = "An error occurred verifying email" });
            }
        }

        [HttpPost("resend-verification")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ResendVerification([FromBody] AuthResendVerificationDto resendVerificationDto)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(resendVerificationDto);
                return Ok(new { message = "If an account exists, a verification email has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email to {Email}", resendVerificationDto.Email);
                return BadRequest(new { message = "An error occurred processing your request" });
            }
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "No token provided" });
                }

                var user = await _authService.GetCurrentUserAsync(token);
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return BadRequest(new { message = "An error occurred getting user information" });
            }
        }

        [HttpGet("validate-token")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "No token provided" });
                }

                var isValid = await _authService.ValidateTokenAsync(token);
                if (isValid)
                {
                    return Ok(new { valid = true, message = "Token is valid" });
                }
                return Unauthorized(new { valid = false, message = "Token is invalid or expired" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return BadRequest(new { message = "An error occurred validating token" });
            }
        }

        private string? GetTokenFromHeader()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return null;
            }

            return authorizationHeader["Bearer ".Length..].Trim();
        }

        private Guid? GetUserIdFromToken(string token)
        {
            // This should use your JWT service to extract user ID
            // For now, returning null - implement properly in real code
            return null;
        }
    }
}