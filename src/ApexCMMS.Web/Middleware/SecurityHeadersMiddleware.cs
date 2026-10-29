namespace ApexCMMS.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Prevent Clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // 2. Prevent MIME Sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 3. Referrer Policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 4. Cross-Site Scripting Protection for legacy browsers
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 5. Permitted Cross-Domain Policies
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
