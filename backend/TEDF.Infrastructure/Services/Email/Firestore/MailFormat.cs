using System.Globalization;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Infrastructure.Services.Email.Firestore;

/// <summary>
/// Renders values for the Vietnamese email templates. Every placeholder is a string, so anything
/// that can be absent is turned into readable copy here rather than reaching the reader as an
/// empty gap in the sentence.
/// </summary>
public static class MailFormat
{
    /// <summary>
    /// Vietnam is UTC+7 all year and observes no daylight saving, so a fixed offset is exact and
    /// avoids depending on a time-zone database id that differs between Windows and Linux hosts.
    /// Timestamps are stored in UTC everywhere in this codebase.
    /// </summary>
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public const string Unknown = "Không xác định";

    public static string Date(DateTime utc) =>
        (utc + VietnamOffset).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string DateTimeText(DateTime utc) =>
        (utc + VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string Text(string? value, string fallback = Unknown) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    /// <summary>The evaluation outcome, phrased for a reader rather than as an enum name.</summary>
    public static string Result(EvaluationResult result) => result switch
    {
        EvaluationResult.Approved => "Duyệt đề tài",
        EvaluationResult.NeedsModification => "Yêu cầu chỉnh sửa",
        EvaluationResult.Rejected => "Không duyệt đề tài",
        _ => "Chưa có kết luận"
    };
}
