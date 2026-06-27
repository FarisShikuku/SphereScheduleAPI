using SphereScheduleAPI.API.Middlewares;

namespace SphereScheduleAPI.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            var env = app.Environment;

            // 1. Global exception handling — must be outermost
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // 2. Security headers
            app.UseSecurityHeaders();

            // 3. Swagger — registered before routing so it is always reachable
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sphere Schedule API v1");
                c.RoutePrefix = "api-docs";   // UI lives at /api-docs
                c.ConfigObject.AdditionalItems["persistAuthorization"] = "true";
            });

            // 4. HTTPS redirection (production only)
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            // 5. CORS — must come before routing
            app.UseCors("AllowAll");

            // 6. Routing
            app.UseRouting();

            // 7. Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // 8. Domain middlewares (run after auth context is set)
            app.UseMiddleware<ActivityLogMiddleware>();
            app.UseMiddleware<JwtMiddleware>();

            // 9. Request logging (dev only, runs last so status codes are known)
            if (env.IsDevelopment())
                app.UseMiddleware<RequestLoggingMiddleware>();

            // 10. Endpoints
            app.MapControllers();
            app.MapHealthChecks("/health");
            app.MapGet("/", (IWebHostEnvironment hostEnv) => Results.Ok(new
            {
                status = "healthy",
                application = "Sphere Schedule API",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = hostEnv.EnvironmentName,
                documentation = "/api-docs",
                health_check = "/health"
            }));

            return app;
        }

        // ─── Security Headers ─────────────────────────────────────────────────
        private static void UseSecurityHeaders(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Remove("Server");
                await next();
            });
        }
    }
}