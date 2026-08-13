namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// One candidate row handed back by a format reader, before the shared field matching runs.
/// </summary>
/// <param name="Text">
/// The whole row flattened to a single string. Used for the "is this a student row?" test, which
/// both formats answer the same way.
/// </param>
/// <param name="Cells">
/// The row split into its cells when the format preserves them (DOCX). The PDF reader cannot
/// recover column boundaries, so it supplies a single cell holding the entire line.
/// </param>
internal readonly record struct RegisterFormRow(string Text, IReadOnlyList<string> Cells);

/// <summary>The section headings both readers key off; kept in one place so they cannot drift apart.</summary>
internal static class RegisterFormHeadings
{
    /// <summary>Opens the student table — printed as "2. Register information for students".</summary>
    public const string StudentSection = "Register information for students";

    /// <summary>Closes it — printed as "3. Register content of Capstone Project".</summary>
    public const string ContentSection = "Register content";
}
