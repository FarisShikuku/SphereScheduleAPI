using System.Diagnostics;
using System.Security.Claims;
using SphereScheduleAPI.Application.Interfaces;

namespace SphereScheduleAPI.API.Middlewares
{
    public class ActivityLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ActivityLogMiddleware> _logger;

        public ActivityLogMiddleware(
            RequestDelegate next,
            ILogger<ActivityLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IActivityLogService activityLogService)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log successful request
                await LogRequestAsync(context, activityLogService, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log failed request
                await LogErrorAsync(context, activityLogService, ex, stopwatch.ElapsedMilliseconds);

                // Restore original body stream and re-throw
                context.Response.Body = originalBodyStream;
                throw;
            }
            finally
            {
                // Copy the response body to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpContext context, IActivityLogService activityLogService, long durationMs)
        {
            try
            {
                var userId = GetUserIdFromContext(context);
                var ipAddress = GetIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var method = context.Request.Method;
                var path = context.Request.Path;
                var statusCode = context.Response.StatusCode;

                // Only log significant requests
                if (ShouldLogRequest(method, path, statusCode))
                {
                    var activityType = GetActivityTypeFromRequest(method, path);
                    var details = $"{method} {path} completed in {durationMs}ms with status {statusCode}";

                    await activityLogService.CreateActivityLogAsync(new Application.DTOs.CreateActivityLogDto
                    {
                        UserId = userId,
                        ActivityType = activityType,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Status = statusCode >= 400 ? "error" : "success",
                        Details = details
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request activity");
            }
        }

        private async Task LogErrorAsync(HttpContext context, IActivityLogService activityLogService, Exception exception, long durationMs)
        {
            try
            {
                var userId = GetUserIdFromContext(context);
                var ipAddress = GetIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var method = context.Request.Method;
                var path = context.Request.Path;

                var activityType = GetActivityTypeFromRequest(method, path);
                var details = $"{method} {path} failed after {durationMs}ms: {exception.Message}";

                await activityLogService.CreateActivityLogAsync(new Application.DTOs.CreateActivityLogDto
                {
                    UserId = userId,
                    ActivityType = activityType,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Status = "error",
                    Details = details
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging error activity");
            }
        }

        private Guid? GetUserIdFromContext(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        private string GetIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool ShouldLogRequest(string method, string path, int statusCode)
        {
            // Don't log health checks, swagger, or static files
            if (path.Contains("/health") ||
                path.Contains("/swagger") ||
                path.Contains(".css") ||
                path.Contains(".js") ||
                path.Contains(".ico"))
            {
                return false;
            }

            // Always log errors
            if (statusCode >= 400)
            {
                return true;
            }

            // Only log significant methods
            return method is "POST" or "PUT" or "DELETE" or "PATCH";
        }

        private string GetActivityTypeFromRequest(string method, string path)
        {
            // Map HTTP methods and paths to activity types
            if (path.Contains("/api/auth/login"))
            {
                return "login";
            }

            if (path.Contains("/api/auth/logout"))
            {
                return "logout";
            }

            // Map CRUD operations
            return method switch
            {
                "POST" => "create",
                "PUT" or "PATCH" => "update",
                "DELETE" => "delete",
                "GET" when path.Contains("/api/export") => "export_data",
                _ => "api_request"
            };
        }
    }
}