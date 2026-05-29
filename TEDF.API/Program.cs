using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using TEDF.API.Common.Security;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application;
using TEDF.Infrastructure;
using TEDF.Infrastructure.RealTime.Hubs;
using TEDF.Persistence;
using System.Text.Json;
using System.Linq;
using System.Threading.RateLimiting;

// Load backend environment variables from .env files before building configuration.
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
DotEnvLoader.LoadForCurrentEnvironment(Directory.GetCurrentDirectory(), environmentName);

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. ADD SERVICES TO THE CONTAINER
// ============================================

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TEDF API",
        Version = "v1",
        Description = "API for TEDF - University Thesis Management System"
    });

    // JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray();

    var allowedOrigins = (configuredOrigins is { Length: > 0 })
        ? configuredOrigins
        : new[] { "http://localhost:3000", "http://localhost:5173", "https://localhost:5173" };

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Upload hard limits (phase 1): reject oversized multipart requests early.
const long proposeTopicMaxUploadBytes = 25 * 1024 * 1024; // 25 MB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = proposeTopicMaxUploadBytes;
});

// Request timeout policies (phase 1): keep slow upload requests from hogging workers.
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("ProposeTopicUploadTimeout", TimeSpan.FromSeconds(60));
});

// Rate limiting (phase 1): reduce request spam on upload/propose endpoint.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("ProposeTopicUploadPolicy", httpContext =>
    {
        var dbUserId = httpContext.User.FindFirst(AppClaimTypes.DbUserId)?.Value;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = !string.IsNullOrWhiteSpace(dbUserId) ? $"user:{dbUserId}" : $"ip:{ip}";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2,
            AutoReplenishment = true,
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var payload = ApiResponse.Fail("Bạn gửi yêu cầu quá nhanh. Vui lòng thử lại sau.");
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.HttpContext.Response.WriteAsync(json, token);
    };
});

// Malware scanning (phase 3): scan uploaded attachments through ClamAV before processing.
builder.Services.Configure<MalwareScanOptions>(
    builder.Configuration.GetSection(MalwareScanOptions.SectionName));
builder.Services.AddScoped<IMalwareScanAuditLogger, MalwareScanAuditLogger>();
builder.Services.AddScoped<IMalwareScanner, MalwareScanner>();
builder.Services.AddScoped<IAttachmentScanWorkflow, AttachmentScanWorkflow>();
builder.Services.AddScoped<AttachmentScanJob>();
builder.Services.AddScoped<QuarantineRetryJob>();

// Layer Services
builder.Services.AddApplicationServices();  // Application Layer (MediatR, Validators, Behaviors)
builder.Services.AddPersistence(builder.Configuration);  // Persistence Layer (EF Core, MongoDB, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);  // Infrastructure Layer (Auth, Email, Caching, etc.)

// ForwardedHeaders: lets the app read X-Forwarded-For / X-Forwarded-Proto
// set by reverse proxies (nginx, IIS, Azure, AWS) so RemoteIpAddress returns
// the real client IP instead of the proxy's IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust all proxies/networks (adjust in prod to only trust your known proxy CIDRs)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ============================================
// 2. INITIALIZE DATABASE
// ============================================
// Apply migrations and seed initial data (runs only once or as needed)
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
}

// ============================================
// 3. CONFIGURE THE HTTP REQUEST PIPELINE
// ============================================

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TEDF API v1");
        options.RoutePrefix = "swagger";
    });
}

// Must be first: extract real client IP from X-Forwarded-For before any middleware reads it
app.UseForwardedHeaders();

// Infrastructure middleware (Correlation ID, Request Logging, Exception Handling, Performance Monitoring)
app.UseInfrastructure();

// Apply request timeout and rate limit middleware before mapping endpoints.
app.UseRequestTimeouts();
app.UseRateLimiter();

// CORS
app.UseCors("AllowFrontend");

// HTTPS Redirection
app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");

// Realtime Hubs
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

// Minimal API Endpoints
app.MapEndpoints();

// Recurring jobs specific to API-layer jobs (not registerable from Infrastructure)
Hangfire.RecurringJob.AddOrUpdate<QuarantineRetryJob>(
    "quarantine-retry",
    job => job.ExecuteAsync(),
    "*/30 * * * *"); // every 30 minutes

app.Run();
