using System.Diagnostics;
using System.Security.Claims;
using SphereScheduleAPI.Application.Interfaces;
using SphereScheduleAPI.Application.DTOs;

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

                    await activityLogService.CreateActivityLogAsync(new CreateActivityLogDto
                    {
                        UserID = userId,
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

                await activityLogService.CreateActivityLogAsync(new CreateActivityLogDto
                {
                    UserID = userId,
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
                            ?? context.User.FindFirst("sub")?.Value
                            ?? context.User.FindFirst("UserID")?.Value;

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
                path.Contains(".ico") ||
                path.Contains("/api-docs"))
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

        /// <summary>
        /// Maps HTTP methods and paths to the exact ActivityType values allowed by the CHECK constraint
        /// Allowed values: 'create', 'login', 'logout', 'create_task', 'update_task', 'delete_task',
        /// 'create_appointment', 'update_appointment', 'delete_appointment', 'create_user', 'update_user',
        /// 'delete_user', 'change_settings', 'export_data', 'share_item'
        /// </summary>
        private string GetActivityTypeFromRequest(string method, string path)
        {
            // Authentication endpoints
            if (path.Contains("/api/auth/login"))
            {
                return "login";
            }

            if (path.Contains("/api/auth/logout"))
            {
                return "logout";
            }

            // Task endpoints
            if (path.Contains("/api/tasks") || path.Contains("/api/Tasks"))
            {
                return method switch
                {
                    "POST" => "create_task",
                    "PUT" or "PATCH" => "update_task",
                    "DELETE" => "delete_task",
                    "GET" when path.Contains("/export") => "export_data",
                    _ => "api_request"
                };
            }

            // Appointment endpoints
            if (path.Contains("/api/appointments") || path.Contains("/api/Appointments"))
            {
                return method switch
                {
                    "POST" => "create_appointment",
                    "PUT" or "PATCH" => "update_appointment",
                    "DELETE" => "delete_appointment",
                    "GET" when path.Contains("/export") => "export_data",
                    _ => "api_request"
                };
            }

            // User endpoints (including registration)
            if (path.Contains("/api/users") || path.Contains("/api/auth/register"))
            {
                return method switch
                {
                    "POST" when path.Contains("/register") => "create_user",
                    "POST" => "create_user",
                    "PUT" or "PATCH" => "update_user",
                    "DELETE" => "delete_user",
                    _ => "api_request"
                };
            }

            // Settings/Preferences endpoints
            if (path.Contains("/api/settings") ||
                path.Contains("/api/preferences") ||
                path.Contains("/api/users/preferences"))
            {
                return "change_settings";
            }

            // Export endpoints
            if (path.Contains("/export"))
            {
                return "export_data";
            }

            // Share endpoints
            if (path.Contains("/share"))
            {
                return "share_item";
            }

            // Category endpoints
            if (path.Contains("/api/categories"))
            {
                return method switch
                {
                    "POST" => "create_task",      // Creating a category is like creating a task
                    "PUT" or "PATCH" => "update_task",
                    "DELETE" => "delete_task",
                    _ => "api_request"
                };
            }

            // Dashboard/Statistics endpoints (read-only)
            if (path.Contains("/api/dashboard") ||
                path.Contains("/api/statistics") ||
                path.Contains("/api/dailystats"))
            {
                return "api_request";
            }

            // Create activity log endpoint (prevent infinite loop)
            if (path.Contains("/api/activitylogs"))
            {
                return "api_request";
            }

            // Default fallback for any other API requests
            return "api_request";
        }
    }
}