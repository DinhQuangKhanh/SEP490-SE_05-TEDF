using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Infrastructure.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, response) = exception switch
            {
                EntityNotFoundException entityNotFoundEx => (HttpStatusCode.NotFound, ApiResponse.Fail(entityNotFoundEx.Message)),
                ValidationException validationEx => (HttpStatusCode.BadRequest, ApiResponse.Fail(validationEx.Message, validationEx.Errors)),
                BusinessRuleValidationException businessRuleEx => (HttpStatusCode.BadRequest, ApiResponse.Fail(businessRuleEx.Message)),
                ConcurrencyException concurrencyEx => (HttpStatusCode.Conflict, ApiResponse.Fail(concurrencyEx.Message)),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, ApiResponse.Fail("Bạn không có quyền truy cập tài nguyên này.")),
                DomainException domainEx => (HttpStatusCode.BadRequest, ApiResponse.Fail(domainEx.Message)),
                ArgumentException argumentEx => (HttpStatusCode.InternalServerError, ApiResponse.Fail(argumentEx.Message)),
                _ => (HttpStatusCode.InternalServerError, ApiResponse.Fail("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."))
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                await LogExceptionAsync(context, exception);
            }
            else
            {
                _logger.LogWarning("Handled exception: {Type} - {Message}", exception.GetType().Name, exception.Message);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json, context.RequestAborted);
        }

        private async Task LogExceptionAsync(HttpContext context, Exception exception)
        {
            try
            {
                var errorLogService = context.RequestServices.GetService<IErrorLogService>();

                var user = context.User;
                var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);

                var userName = user.FindFirstValue(ClaimTypes.Name)
                               ?? user.FindFirstValue("name")
                               ?? "Anonymous";
                var userEmail = user.FindFirstValue(ClaimTypes.Email);

                var apiPath = context.Request.Path.Value ?? "";
                var httpMethod = context.Request.Method;
                var role = ResolveRoleFromPath(apiPath);
                var routePath = context.Request.Headers["X-Route-Path"].ToString();
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var correlationId = context.TraceIdentifier;

                var innerExceptions = new List<InnerExceptionEntry>();
                var inner = exception.InnerException;
                while (inner is not null)
                {
                    innerExceptions.Add(new InnerExceptionEntry(
                        inner.Message,
                        inner.GetType().FullName ?? inner.GetType().Name,
                        inner.StackTrace));
                    inner = inner.InnerException;
                }

                if (errorLogService is not null)
                {
                    await errorLogService.LogAsync(new ErrorLogEntry(
                        UserId: userIdStr,
                        UserName: userName,
                        UserEmail: userEmail,
                        ActiveRole: role,
                        Severity: "critical",
                        Source: "Middleware",
                        ActionCode: "UnhandledException",
                        ActionDisplayName: $"Lỗi hệ thống: {httpMethod} {apiPath}",
                        RoutePath: string.IsNullOrEmpty(routePath) ? null : routePath,
                        RequestPath: apiPath,
                        RequestMethod: httpMethod,
                        IpAddress: ipAddress,
                        UserAgent: userAgent,
                        ErrorMessage: exception.Message,
                        ErrorType: exception.GetType().FullName ?? exception.GetType().Name,
                        StackTrace: exception.StackTrace,
                        InnerExceptions: innerExceptions,
                        RequestParameters: null,
                        CorrelationId: correlationId,
                        Timestamp: DateTime.UtcNow
                    ),
                    // Deliberately not tied to context.RequestAborted: the error must still be
                    // persisted when the client disconnects mid-request.
                    CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist unhandled exception to logs");
            }
        }

        private static string ResolveRoleFromPath(string path)
        {
            if (path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase)) return "admin";
            if (path.StartsWith("/api/mentor/", StringComparison.OrdinalIgnoreCase)) return "mentor";
            if (path.StartsWith("/api/evaluator/", StringComparison.OrdinalIgnoreCase)) return "evaluator";
            return path.StartsWith("/api/student/", StringComparison.OrdinalIgnoreCase) ? "student" : "anonymous";
        }
    }
}
