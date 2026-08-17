using System.Text;
using System.Text.RegularExpressions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Services.RegisterForm;

/// <summary>
/// Turns a format-neutral <see cref="RegisterFormDoc"/> (lines + tables) into the
/// <see cref="RegisterFormContent"/> the propose flow validates and maps. All extraction is
/// best-effort: a field that cannot be located comes back null/empty. Runs identically for DOCX / DOC
/// / PDF because it only ever sees lines and tables — the readers absorb the format differences.
/// <para>
/// The PDF export in particular wraps every field across several visual lines and loses table
/// structure, so this extractor treats a section as a run of lines and rejoins wrapped fragments:
/// titles and technology names are space-joined so a word split across two lines is made whole, while
/// the product-kind checkbox grid (tail of 3.3) and the signature block (tail of 3.4) are trimmed off.
/// </para>
/// </summary>
internal static partial class RegisterFormContentExtractor
{
    // Ticked vs empty checkbox glyphs seen across Word/PDF exports of the form.
    private static readonly string[] CheckedGlyphs = ["🗹", "☑", "☒", "✅", "✔", "✓"];
    private static readonly string[] UncheckedGlyphs = ["☐", "□", "⬜", "▢"];

    // Bullet glyphs the PDF prints in front of a "Label:" line inside section 3.2.
    private static readonly char[] BulletChars = ['●', '•', '▪', '◦', '‣', '·', '*', '-', '–', '—'];

    // The 3.1 field labels; each field's value continues onto later lines until the next of these.
    private static readonly string[] TitleLabels = ["English:", "Vietnamese:", "Abbreviation:", "Abbreviate:"];

    public static RegisterFormContent Extract(RegisterFormDoc doc)
    {
        var lines = doc.Lines;

        var (nameEn, nameVi, nameAbbr) = Parse31(SectionLines(lines, "3.1", "3.2"));
        // Fallback for exports without a clear "3.1"/"3.2" heading: scan the whole document (single line).
        nameEn ??= FirstLabelValue(lines, "English:");
        nameVi ??= FirstLabelValue(lines, "Vietnamese:");
        nameAbbr ??= StripHint(FirstLabelValue(lines, "Abbreviation:", "Abbreviate:"));

        var (description, objectives, technologies) = Parse32(SectionLines(lines, "3.2", "3.3"));
        var expectedResults = Parse33(TrimAtProductKinds(SectionLines(lines, "3.3", "3.4")));
        var scope = Parse34(TrimAtSignature(SectionLines(lines, "3.4", null)));

        return new RegisterFormContent(
            Supervisors: ExtractSupervisors(doc),
            LecturerRegisterTicked: FindLecturerTicked(lines),
            NameEn: nameEn,
            NameVi: nameVi,
            NameAbbr: nameAbbr,
            Description: description,
            Objectives: objectives,
            Technologies: technologies,
            ExpectedResults: expectedResults,
            Scope: scope,
            Roster: ExtractRoster(doc));
    }

    // ── "Kinds of person make registers" checkbox ──────────────────────────────────
    private static bool? FindLecturerTicked(IReadOnlyList<string> lines)
    {
        var line = lines.FirstOrDefault(l => l.Contains("Kinds of person", StringComparison.OrdinalIgnoreCase))
                   ?? lines.FirstOrDefault(l =>
                       l.Contains("Lecturer", StringComparison.OrdinalIgnoreCase) &&
                       l.Contains("Students", StringComparison.OrdinalIgnoreCase));
        if (line is null)
            return null;

        var lecturerAt = line.IndexOf("Lecturer", StringComparison.OrdinalIgnoreCase);
        if (lecturerAt < 0)
            return null;

        // The tick sits right after "Lecturer:", before "Students". Read only that slice so the
        // Students box can never be mistaken for the Lecturer one.
        var studentsAt = line.IndexOf("Students", lecturerAt + 1, StringComparison.OrdinalIgnoreCase);
        var segment = studentsAt > lecturerAt ? line[lecturerAt..studentsAt] : line[lecturerAt..];

        var checkedAt = FirstIndexOfAny(segment, CheckedGlyphs);
        var uncheckedAt = FirstIndexOfAny(segment, UncheckedGlyphs);
        if (checkedAt < 0 && uncheckedAt < 0)
            return null;                            // no box read → caller treats as "not ticked"
        if (uncheckedAt < 0)
            return true;
        if (checkedAt < 0)
            return false;
        return checkedAt < uncheckedAt;             // whichever glyph comes first wins
    }

    private static int FirstIndexOfAny(string text, string[] needles)
    {
        var best = -1;
        foreach (var needle in needles)
        {
            var at = text.IndexOf(needle, StringComparison.Ordinal);
            if (at >= 0 && (best < 0 || at < best))
                best = at;
        }
        return best;
    }

    // ── Section 1 supervisors ──────────────────────────────────────────────────────
    private static IReadOnlyList<RegisterFormSupervisor> ExtractSupervisors(RegisterFormDoc doc)
    {
        var result = new List<RegisterFormSupervisor>();

        var table = doc.Tables.FirstOrDefault(t =>
            t.Any(r => r.Count > 0 && SupervisorRowRegex().IsMatch(r[0])));

        if (table is not null)
        {
            foreach (var row in table)
            {
                if (row.Count == 0 || !SupervisorRowRegex().IsMatch(row[0]))
                    continue;

                var name = row.Count > 1 && row[1].Length > 0 ? row[1] : null;
                var email = RegisterRowMatcher.FindEmail(string.Join(' ', row));
                var title = row[^1].Length > 0 && row[^1] != name ? row[^1] : null;
                if (name is not null || email is not null)
                    result.Add(new RegisterFormSupervisor(name, email, title));
            }
            return result;
        }

        // Lines-only (PDF/DOC): the table collapsed, so match the supervisor row and read its e-mail.
        foreach (var line in doc.Lines)
        {
            if (!SupervisorRowRegex().IsMatch(line))
                continue;
            var email = RegisterRowMatcher.FindEmail(line);
            if (email is not null)
                result.Add(new RegisterFormSupervisor(null, email, null));
        }
        return result;
    }

    // ── Section 2 roster (reuses the shared row matcher) ────────────────────────────
    private static IReadOnlyList<RegisterRosterRow> ExtractRoster(RegisterFormDoc doc)
    {
        var rows = new List<RegisterRosterRow>();

        var table = doc.Tables.FirstOrDefault(t =>
            t.Any(r => r.Count > 0 && RegisterRowMatcher.IsStudentRow(r[0])));

        if (table is not null)
        {
            foreach (var row in table)
            {
                if (row.Count == 0 || !RegisterRowMatcher.IsStudentRow(row[0]))
                    continue;

                var joined = string.Join(' ', row);
                var code = RegisterRowMatcher.FindStudentCode(joined);
                var email = RegisterRowMatcher.FindEmail(joined);
                if (code is null && email is null)
                    continue;

                var isLeader = row.Any(c => !IsIdentifierCell(c, code, email) && RegisterRowMatcher.IsLeaderText(c));
                rows.Add(new RegisterRosterRow(code, email, isLeader));
            }
        }
        else
        {
            foreach (var line in doc.Lines)
            {
                if (!RegisterRowMatcher.IsStudentRow(line))
                    continue;
                var code = RegisterRowMatcher.FindStudentCode(line);
                var email = RegisterRowMatcher.FindEmail(line);
                if (code is null && email is null)
                    continue;
                rows.Add(new RegisterRosterRow(code, email, RegisterRowMatcher.IsLeaderText(line)));
            }
        }

        if (rows.Count > 0 && !rows.Any(r => r.IsLeader))
            rows[0] = rows[0] with { IsLeader = true };
        return rows;
    }

    private static bool IsIdentifierCell(string cell, string? code, string? email) =>
        (code is not null && RegisterRowMatcher.FindStudentCode(cell) == code)
        || (email is not null && RegisterRowMatcher.FindEmail(cell) == email);

    // ── 3.1 Title → English / Vietnamese / Abbreviation ─────────────────────────────
    private static (string? En, string? Vi, string? Abbr) Parse31(IReadOnlyList<string> s)
    {
        if (s.Count == 0)
            return (null, null, null);

        var en = LabelValueContinued(s, ["English:"]);
        var vi = LabelValueContinued(s, ["Vietnamese:"]);
        // The abbreviation is a single token followed by a parenthetical hint that itself wraps onto the
        // next line — take only the label line and cut the hint, never the wrapped hint remainder.
        var abbr = StripHint(FirstLabelValue(s, "Abbreviation:", "Abbreviate:"));
        return (en, vi, abbr);
    }

    // ── 3.2 Context → Description (brief intro), Objectives, Technologies ────────────
    private static (string? Description, string? Objectives, IReadOnlyList<string> Technologies) Parse32(IReadOnlyList<string> s)
    {
        var objIdx = IndexOfLabel(s, "Objectives:", "Objective:");
        var techIdx = IndexOfLabel(s, "Technology/algorithm:", "Technology / algorithm:", "Technology:", "Algorithm:");

        var descEnd = objIdx >= 0 ? objIdx : (techIdx >= 0 ? techIdx : s.Count);
        var description = JoinRange(s, 0, descEnd);

        string? objectives = null;
        if (objIdx >= 0)
        {
            var objEnd = techIdx > objIdx ? techIdx : s.Count;
            objectives = JoinLabelBlock(s, objIdx, objEnd);
        }

        return (description, objectives, ParseTechnologies(s, techIdx));
    }

    /// <summary>
    /// The technology block spans several labelled, bulleted lines (Front-end / Back-end / …) and each
    /// label's value wraps onto unbulleted continuation lines. Rejoin wrapped fragments with a space
    /// (so "SignalR" + "Client" and "Event-Driven" + "Architecture" become whole), drop the label
    /// prefixes and the multi-line "(GVHD…)" hint, then split on commas and de-duplicate.
    /// </summary>
    private static IReadOnlyList<string> ParseTechnologies(IReadOnlyList<string> s, int techIdx)
    {
        if (techIdx < 0)
            return [];

        var buf = new StringBuilder();
        for (var i = techIdx; i < s.Count; i++)
        {
            var trimmed = s[i].TrimStart();
            var noBullet = trimmed.TrimStart(BulletChars).TrimStart();

            if (i == techIdx || StartsWithBullet(trimmed) || LooksLikeLabel(noBullet))
            {
                // New labelled group: keep only what follows the label's colon, comma-separated
                // from the previous group so the two groups' items never merge.
                var colon = noBullet.IndexOf(':');
                buf.Append(", ").Append(colon >= 0 ? noBullet[(colon + 1)..] : noBullet);
            }
            else
            {
                // Wrapped continuation of the current group — a space rejoins a split word.
                buf.Append(' ').Append(trimmed);
            }
        }

        var joined = StripParens(buf.ToString());
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var techs = new List<string>();
        foreach (var part in joined.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (part.Length > 0 && seen.Add(part))
                techs.Add(part);
        return techs;
    }

    private static bool StartsWithBullet(string trimmed) =>
        trimmed.Length > 0 && Array.IndexOf(BulletChars, trimmed[0]) >= 0;

    /// <summary>A "Front-end:" / "Kiến trúc:" style label — letters before an early colon, no comma first.</summary>
    private static bool LooksLikeLabel(string noBullet)
    {
        var colon = noBullet.IndexOf(':');
        if (colon <= 0)
            return false;
        var comma = noBullet.IndexOf(',');
        if (comma >= 0 && comma < colon)
            return false;                           // "React, Redux: …" would be content, not a label
        return LabelPrefixRegex().IsMatch(noBullet[..colon]);
    }

    // ── 3.3 → ExpectedResults (the "expected outputs" portion) ──────────────────────
    private static string? Parse33(IReadOnlyList<string> s)
    {
        var splitIdx = IndexOfLabel(s, "Sản phẩm dự kiến", "Expected output", "Expected outputs", "Expected product");
        if (splitIdx >= 0)
        {
            var block = JoinLabelBlock(s, splitIdx, s.Count);
            if (!string.IsNullOrWhiteSpace(block))
                return block;
        }
        return JoinRange(s, 0, s.Count);   // fallback: the whole of 3.3
    }

    // ── 3.4 → Scope ─────────────────────────────────────────────────────────────────
    private static string? Parse34(IReadOnlyList<string> s) => JoinRange(s, 0, s.Count);

    // ── Section-tail trimming (PDF bleeds these into the last field of a section) ────
    /// <summary>Cuts the "Website / Mobile / Game …" product-kind checkbox grid off the end of 3.3.</summary>
    private static IReadOnlyList<string> TrimAtProductKinds(IReadOnlyList<string> s) =>
        TrimFrom(s, l =>
            l.Contains("Website application", StringComparison.OrdinalIgnoreCase)
            || l.Contains("Mobile application", StringComparison.OrdinalIgnoreCase)
            || l.Contains("Kinds of product", StringComparison.OrdinalIgnoreCase));

    /// <summary>Cuts the "Da Nang, dd/mm/yyyy … Sign and full name" signature block off the end of 3.4.</summary>
    private static IReadOnlyList<string> TrimAtSignature(IReadOnlyList<string> s) =>
        TrimFrom(s, l =>
            l.Contains("On behalf of Registers", StringComparison.OrdinalIgnoreCase)
            || l.Contains("Sign and full name", StringComparison.OrdinalIgnoreCase)
            || l.Contains("Supervisor (If have)", StringComparison.OrdinalIgnoreCase)
            || SignatureDateRegex().IsMatch(l));

    private static IReadOnlyList<string> TrimFrom(IReadOnlyList<string> lines, Func<string, bool> isBoundary)
    {
        for (var i = 0; i < lines.Count; i++)
            if (isBoundary(lines[i]))
                return [.. lines.Take(i)];
        return lines;
    }

    // ── Line / label helpers ────────────────────────────────────────────────────────
    private static string? FirstLabelValue(IReadOnlyList<string> lines, params string[] labels)
    {
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            foreach (var label in labels)
                if (trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    var value = trimmed[label.Length..].Trim();
                    if (value.Length > 0)
                        return value;
                }
        }
        return null;
    }

    /// <summary>
    /// The value of <paramref name="labels"/> plus every following wrapped line, space-joined, stopping
    /// at the next 3.1 label or a section heading. Used for the title fields, which wrap in the PDF.
    /// </summary>
    private static string? LabelValueContinued(IReadOnlyList<string> lines, string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            var matched = labels.FirstOrDefault(l => trimmed.StartsWith(l, StringComparison.OrdinalIgnoreCase));
            if (matched is null)
                continue;

            var parts = new List<string>();
            var head = trimmed[matched.Length..].Trim();
            if (head.Length > 0)
                parts.Add(head);

            for (var j = i + 1; j < lines.Count; j++)
            {
                var next = lines[j].TrimStart();
                if (StartsWithAny(next, TitleLabels) || IsSectionHeading(next))
                    break;
                parts.Add(next);
            }

            var joined = string.Join(' ', parts).Trim();
            return joined.Length > 0 ? joined : null;
        }
        return null;
    }

    private static bool StartsWithAny(string text, string[] labels) =>
        labels.Any(l => text.StartsWith(l, StringComparison.OrdinalIgnoreCase));

    private static bool IsSectionHeading(string trimmed) =>
        SectionHeadingRegex().IsMatch(trimmed);

    private static int IndexOfLabel(IReadOnlyList<string> lines, params string[] labels)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart().TrimStart(BulletChars).TrimStart();
            foreach (var label in labels)
                if (trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                    return i;
        }
        return -1;
    }

    /// <summary>The value after the label on line <paramref name="labelIdx"/> plus every line up to
    /// <paramref name="end"/>, joined by newline. Used for multi-line blocks like Objectives.</summary>
    private static string? JoinLabelBlock(IReadOnlyList<string> lines, int labelIdx, int end)
    {
        var parts = new List<string>();
        var head = lines[labelIdx];
        var rhs = head.Contains(':') ? head[(head.IndexOf(':') + 1)..].Trim() : string.Empty;
        if (rhs.Length > 0)
            parts.Add(rhs);
        for (var i = labelIdx + 1; i < end; i++)
            parts.Add(lines[i]);
        var joined = string.Join('\n', parts).Trim();
        return joined.Length > 0 ? joined : null;
    }

    private static string? JoinRange(IReadOnlyList<string> lines, int from, int to)
    {
        if (from >= to)
            return null;
        var joined = string.Join('\n', lines.Skip(from).Take(to - from)).Trim();
        return joined.Length > 0 ? joined : null;
    }

    /// <summary>Lines strictly between the section heading (e.g. "3.2") and the next one (e.g. "3.3").</summary>
    private static IReadOnlyList<string> SectionLines(IReadOnlyList<string> lines, string startTag, string? endTag)
    {
        var start = IndexOfHeading(lines, 0, startTag);
        if (start < 0)
            return [];
        var end = endTag is null ? lines.Count : IndexOfHeading(lines, start + 1, endTag);
        if (end < 0)
            end = lines.Count;

        var result = new List<string>();
        for (var i = start + 1; i < end; i++)
            result.Add(lines[i]);
        return result;
    }

    private static int IndexOfHeading(IReadOnlyList<string> lines, int from, string tag)
    {
        for (var i = from; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(tag + ".", StringComparison.Ordinal)
                || trimmed.StartsWith(tag + " ", StringComparison.Ordinal)
                || trimmed == tag)
                return i;
        }
        return -1;
    }

    private static string? StripHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        var paren = value.IndexOf('(');
        var stripped = (paren >= 0 ? value[..paren] : value).Trim();
        return stripped.Length > 0 ? stripped : null;
    }

    private static string StripParens(string text) => ParensRegex().Replace(text, " ");

    [GeneratedRegex(@"\bSupervisor\s*\d\b", RegexOptions.IgnoreCase)]
    private static partial Regex SupervisorRowRegex();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex ParensRegex();

    [GeneratedRegex(@"^[\p{L}][\p{L}\s/&.\-]{0,40}$")]
    private static partial Regex LabelPrefixRegex();

    [GeneratedRegex(@"^\d+(\.\d+)*[.\s]")]
    private static partial Regex SectionHeadingRegex();

    [GeneratedRegex(@"\d{1,2}\s*/\s*\d{1,2}\s*/\s*\d{4}\s*$")]
    private static partial Regex SignatureDateRegex();
}
