using System.Security.Claims;
using SphereScheduleAPI.Infrastructure.Services;

namespace SphereScheduleAPI.API.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        // JwtService is NOT injected here — middleware is instantiated once (singleton-like),
        // so scoped services must be resolved per-request via IServiceProvider.
        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, JwtService jwtService)
        {
            // ASP.NET Core injects method parameters from the per-request scope — safe for scoped services.
            var token = context.Request.Headers["Authorization"]
                                       .FirstOrDefault()
                                       ?.Split(" ")
                                       .Last();

            if (token is not null)
                AttachUserToContext(context, token, jwtService);

            await _next(context);
        }

        private static void AttachUserToContext(HttpContext context, string token, JwtService jwtService)
        {
            try
            {
                var principal = jwtService.ValidateToken(token);
                if (principal is null) return;

                context.User = principal;

                var UserIDClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? principal.FindFirst("UserID")?.Value;

                if (!Guid.TryParse(UserIDClaim, out var UserID)) return;

                context.Items["UserID"] = UserID;
                context.Items["UserEmail"] = principal.FindFirst(ClaimTypes.Email)?.Value;
                context.Items["UserRoles"] = principal.FindAll(ClaimTypes.Role)
                                                      .Select(c => c.Value)
                                                      .ToList();
            }
            catch
            {
                // Token validation failed — let the request continue unauthenticated.
                // UseAuthentication() will enforce auth on protected endpoints.
            }
        }
    }
}