using System.Text.RegularExpressions;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// The field-recognition rules of the register form, shared by the PDF and DOCX readers so both
/// formats agree on what counts as a student row, a student code, an e-mail or the leader.
/// </summary>
internal static partial class RegisterRowMatcher
{
    /// <summary>True when the text carries one of the template's row labels ("Student 1" … "Student 5").</summary>
    public static bool IsStudentRow(string text) => StudentRowRegex().IsMatch(text);

    public static string? FindStudentCode(string text)
    {
        var match = StudentCodeRegex().Match(text);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    public static string? FindEmail(string text)
    {
        var match = EmailRegex().Match(text);
        return match.Success ? match.Value.ToLowerInvariant() : null;
    }

    public static bool IsLeaderText(string text) =>
        text.Contains("leader", StringComparison.OrdinalIgnoreCase);

    /// <summary>Matches the row labels the template prints: "Student 1" … "Student 5".</summary>
    [GeneratedRegex(@"\bStudent\s*\d\b", RegexOptions.IgnoreCase)]
    private static partial Regex StudentRowRegex();

    /// <summary>
    /// Student codes are letters followed by digits (e.g. HE160123, LT000033). Requiring the leading
    /// letters keeps phone numbers — which are digits only — from being mistaken for a code.
    /// </summary>
    [GeneratedRegex(@"\b[A-Za-z]{2,3}\d{4,8}\b")]
    private static partial Regex StudentCodeRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b")]
    private static partial Regex EmailRegex();
}
