using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.Services;

/// <summary>
/// Persists error log entries into the <c>error_logs</c> MongoDB collection.
/// </summary>
public class ErrorLogService : IErrorLogService
{
    private readonly IErrorLogRepository _errorLogRepository;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(IErrorLogRepository errorLogRepository, ILogger<ErrorLogService> logger)
    {
        _errorLogRepository = errorLogRepository;
        _logger = logger;
    }

    public async Task LogAsync(ErrorLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            _ = Guid.TryParse(entry.UserId, out var userId);

            var document = new ErrorLogDocument
            {
                UserId = userId == Guid.Empty ? null : userId,
                UserName = entry.UserName,
                UserEmail = entry.UserEmail,
                ActiveRole = entry.ActiveRole,
                Severity = entry.Severity ?? "error",
                Source = entry.Source,
                ActionCode = entry.ActionCode,
                Action = entry.ActionDisplayName,
                RoutePath = entry.RoutePath,
                RequestPath = entry.RequestPath,
                RequestMethod = entry.RequestMethod,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                Timestamp = entry.Timestamp,
                ErrorMessage = entry.ErrorMessage,
                ErrorType = entry.ErrorType,
                StackTrace = entry.StackTrace,
                InnerExceptions = entry.InnerExceptions
                    .Select(ie => new InnerExceptionDetail
                    {
                        Message = ie.Message,
                        Type = ie.Type,
                        StackTrace = ie.StackTrace
                    })
                    .ToList(),
                RequestParameters = BuildParameters(entry.RequestParameters),
                CorrelationId = entry.CorrelationId
            };

            await _errorLogRepository.AddAsync(document, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist error log for {ActionCode} to MongoDB", entry.ActionCode);
        }
    }

    private static BsonDocument? BuildParameters(Dictionary<string, object?>? parameters)
    {
        if (parameters is not { Count: > 0 }) return null;

        var doc = new BsonDocument();
        foreach (var (key, value) in parameters)
        {
            doc[key] = value switch
            {
                null => BsonNull.Value,
                int i => new BsonInt32(i),
                long l => new BsonInt64(l),
                bool b => new BsonBoolean(b),
                double d => new BsonDouble(d),
                Guid g => new BsonString(g.ToString()),
                DateTime dt => new BsonDateTime(dt),
                _ => new BsonString(value.ToString() ?? string.Empty),
            };
        }
        return doc;
    }
}
