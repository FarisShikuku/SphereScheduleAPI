using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SphereScheduleAPI.Infrastructure.Services;

namespace SphereScheduleAPI.API.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwtService _jwtService;

        public JwtMiddleware(RequestDelegate next, JwtService jwtService)
        {
            _next = next;
            _jwtService = jwtService;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                await AttachUserToContext(context, token);
            }

            await _next(context);
        }

        private async Task AttachUserToContext(HttpContext context, string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal != null)
                {
                    context.User = principal;

                    // Extract user ID and add to HttpContext items for easy access
                    var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? principal.FindFirst("userId")?.Value;

                    if (Guid.TryParse(userIdClaim, out var userId))
                    {
                        context.Items["UserId"] = userId;
                        context.Items["UserEmail"] = principal.FindFirst(ClaimTypes.Email)?.Value;
                        context.Items["UserRoles"] = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                    }
                }
            }
            catch
            {
                // Token validation failed - do nothing
            }
        }
    }
}