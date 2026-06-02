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

builder.Services.AddSecurityServices(builder.Configuration);
builder.Services.AddJobServices();
builder.Services.AddSignalRServices();
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UniThesis API v1");
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

// Maintenance mode: block non-admin API calls with 503 when enabled (after auth so roles are known)
app.UseMaintenanceMode();

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
