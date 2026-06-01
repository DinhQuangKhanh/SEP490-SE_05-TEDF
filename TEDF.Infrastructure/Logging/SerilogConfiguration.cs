using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace TEDF.Infrastructure.Logging
{
    public static class SerilogConfiguration
    {
        public static void ConfigureSerilog(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Application", "TEDF")
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/TEDF-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            builder.Host.UseSerilog();
        }

        /// <summary>
        /// Configures Serilog request logging with host, user-agent, IP and user ID enrichment.
        /// </summary>
        public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder app)
        {
            return Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
                    diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress);
                };
            });
        }
    }
}
