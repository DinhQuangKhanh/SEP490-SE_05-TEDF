using Microsoft.AspNetCore.HttpOverrides;
using System.Linq;
using TEDF.API.Common.Security.Jobs;
using TEDF.API.Extensions;
using TEDF.Application;
using TEDF.Infrastructure;
using TEDF.Infrastructure.Middleware;
using TEDF.Infrastructure.RealTime.Hubs;
using TEDF.Persistence;

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
        Title = "UniThesis API",
        Version = "v1",
        Description = "API for UniThesis - University Thesis Management System"
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
        .Select(origin => origin.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    var allowedOrigins = configuredOrigins;

    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Dev fallback: allow localhost/127.0.0.1 from any port to avoid silent CORS blocks
            policy.SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            });
        }

        policy.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddSecurityServices(builder.Configuration);
builder.Services.AddJobServices();
builder.Services.AddSignalRServices(builder.Configuration);
builder.Services.AddHealthCheckServices();

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
// Development applies migrations and seeds the full load-test dataset; every other environment only
// tops up the reference data the app cannot run without (Departments + Majors) and never touches
// real records. See InitializeDatabaseAsync for the exact split.
await app.Services.InitializeDatabaseAsync(app.Environment.IsDevelopment());

// ============================================
// 3. CONFIGURE THE HTTP REQUEST PIPELINE
// ============================================

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UniThesis API v1");
        options.RoutePrefix = "swagger";
    });
}

// Must be first: extract real client IP from X-Forwarded-For before any middleware reads it
app.UseForwardedHeaders();

// HTTPS Redirection — development only.
// In Production the container listens on HTTP alone (ASPNETCORE_URLS=http://+:8080) and TLS is
// terminated by the nginx reverse proxy, so this middleware can only do harm: any request that
// reaches the app without X-Forwarded-Proto=https gets redirected to its own https URL, which the
// proxy forwards back over http — an infinite redirect loop (ERR_TOO_MANY_REDIRECTS) that browsers
// then cache. Let nginx own the http→https redirect instead.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Infrastructure middleware (Correlation ID, Request Logging, Exception Handling, Performance Monitoring)
app.UseInfrastructure();

// CORS
if (app.Environment.IsProduction())
{
    var hasConfiguredOrigins = app.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Any(origin => !string.IsNullOrWhiteSpace(origin)) ?? false;

    if (!hasConfiguredOrigins)
    {
        app.Logger.LogWarning(
            "Cors:AllowedOrigins is empty in Production. No browser origin will be allowed to call this API " +
            "(SignalR hubs included) until Cors__AllowedOrigins__0 (and __1, ...) are set.");
    }
}

app.UseCors("AllowFrontend");

// Apply request timeout and rate limit middleware before mapping endpoints.
app.UseRequestTimeouts();
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Maintenance mode: block non-admin API calls with 503 when enabled (after auth so roles are known)
app.UseMaintenanceMode();

// Account access gate: block locked/inactive accounts and ineligible students (after auth so identity is known)
app.UseAccountAccessGate();

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
