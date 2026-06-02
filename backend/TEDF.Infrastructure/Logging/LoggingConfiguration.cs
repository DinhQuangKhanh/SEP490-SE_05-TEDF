using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Configuration helpers for logging infrastructure.
    /// </summary>
    public static class LoggingConfiguration
    {
        /// <summary>
        /// Configures logging services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddLoggingInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var loggingSettings = configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings();

            // Configure Serilog
            Log.Logger = CreateLogger(loggingSettings, configuration);

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);

                // Set minimum level from configuration
                if (Enum.TryParse<LogLevel>(loggingSettings.MinimumLevel, out var minLevel))
                {
                    builder.SetMinimumLevel(minLevel);
                }
            });

            return services;
        }

        /// <summary>
        /// Uses logging middleware in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseLoggingInfrastructure(this IApplicationBuilder app)
        {
            // Add request logging
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                    diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                    }
                };

                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            });

            return app;
        }

        private static Serilog.ILogger CreateLogger(LoggingSettings settings, IConfiguration configuration)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            // Set minimum level
            var minLevel = settings.MinimumLevel?.ToLower() switch
            {
                "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            loggerConfig.MinimumLevel.Is(minLevel);

            // Override specific namespaces
            loggerConfig
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Information);

            // Enrich logs
            loggerConfig
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "TEDF")
                .Enrich.WithProperty("Version", GetAssemblyVersion());

            // Console sink
            if (settings.EnableConsole)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}");
            }

            // File sink
            if (settings.EnableFile && !string.IsNullOrEmpty(settings.FilePath))
            {
                loggerConfig.WriteTo.File(
                    path: settings.FilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: settings.RetainedFileCountLimit,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: settings.FileSizeLimitMb * 1024 * 1024,
                    rollOnFileSizeLimit: true);
            }

            // JSON file sink for structured logging
            if (settings.EnableJsonFile && !string.IsNullOrEmpty(settings.JsonFilePath))
            {
                loggerConfig.WriteTo.File(
                    new CompactJsonFormatter(),
                    path: settings.JsonFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: settings.RetainedFileCountLimit);
            }
            return loggerConfig.CreateLogger();
        }

        private static string GetAssemblyVersion()
        {
            return typeof(LoggingConfiguration).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        }
    }
}
