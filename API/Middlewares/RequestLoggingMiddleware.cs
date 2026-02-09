using System.Diagnostics;

namespace SphereScheduleAPI.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            var correlationId = GetCorrelationId(context);

            // Log request start
            _logger.LogInformation(
                "Request started: {Method} {Path} {QueryString} | CorrelationId: {CorrelationId} | ClientIP: {ClientIP}",
                request.Method,
                request.Path,
                request.QueryString,
                correlationId,
                GetClientIp(context));

            // Store correlation ID for downstream use
            context.Items["CorrelationId"] = correlationId;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var response = context.Response;

                // Determine log level based on status code
                var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                               response.StatusCode >= 400 ? LogLevel.Warning :
                               LogLevel.Information;

                _logger.Log(logLevel,
                    "Request completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    request.Method,
                    request.Path,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
        }

        private static string GetCorrelationId(HttpContext context)
        {
            // Try to get correlation ID from header
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                return correlationId.ToString();
            }

            // Try to get from trace identifier
            if (!string.IsNullOrEmpty(context.TraceIdentifier))
            {
                return context.TraceIdentifier;
            }

            // Generate new correlation ID
            return Guid.NewGuid().ToString();
        }

        private static string GetClientIp(HttpContext context)
        {
            // Check for forwarded headers (proxies, load balancers)
            var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                return forwardedHeader.Split(',').First().Trim();
            }

            // Check for remote IP address
            var remoteIp = context.Connection.RemoteIpAddress;
            return remoteIp?.ToString() ?? "unknown";
        }
    }
}