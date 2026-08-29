using Microsoft.Extensions.Logging.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Infrastructure.Services.RegisterForm;
using Xunit;

namespace TEDF.Tests;

/// <summary>
/// Parses the real filled "Capstone Project Register" fixtures (DOCX + its PDF export) through the
/// public <see cref="RegisterFormParser.ExtractContent"/> and asserts the golden values. The PDF path
/// is the important one: its export wraps every field across visual lines and loses table structure,
/// so these tests guard that the extractor (a) recovers the supervisor e-mail — the a1 regression,
/// (b) rejoins wrapped titles / technology names instead of truncating them, and (c) trims the
/// product-kind checkbox grid and the signature block that otherwise bleed into 3.3 / 3.4.
/// </summary>
public class RegisterFormParserTests
{
    private const string FullTitleEn =
        "Building an Evaluation Framework for Detecting Duplicate Thesis Topics Based on " +
        "Knowledge Domain Awareness in Software Engineering at FPT University Da Nang";

    private static RegisterFormContent Parse(string fixture)
    {
        var resource = $"TEDF.Tests.Fixtures.{fixture}";
        using var stream = typeof(RegisterFormParserTests).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded fixture '{resource}' not found.");
        return new RegisterFormParser(NullLogger<RegisterFormParser>.Instance).ExtractContent(stream);
    }

    // The two exports of the same form must extract to the same content (bar the PDF losing the
    // supervisor/student names, which its table structure cannot preserve).
    [Theory]
    [InlineData("register-form-lecturer.docx")]
    [InlineData("register-form-lecturer.pdf")]
    public void Reads_every_field_without_wrapping_or_bleed(string fixture)
    {
        var content = Parse(fixture);

        Assert.True(content.LecturerRegisterTicked);

        // 3.1 — the titles wrap onto a second line in the PDF; they must be whole, not truncated.
        Assert.Equal(FullTitleEn, content.NameEn);
        Assert.Contains("Đại học FPT Đà Nẵng", content.NameVi);
        Assert.DoesNotContain("...", content.NameEn);           // no ellipsis / cut marker
        Assert.Equal("TEDF", content.NameAbbr);                 // hint in parentheses stripped

        // 3.2 — description is the brief intro only; objectives + technologies excluded from it.
        Assert.Contains("Trong bối cảnh", content.Description);
        Assert.DoesNotContain("Objectives", content.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(content.Objectives));

        // 3.2 technologies — wrapped names rejoined, label/hint dropped, duplicates removed.
        Assert.Contains("React 19", content.Technologies);
        Assert.Contains("GitHub Actions", content.Technologies);
        Assert.Contains("SignalR Client", content.Technologies);          // "SignalR" + "Client" rejoined
        Assert.Contains("Event-Driven Architecture", content.Technologies); // "Event-Driven" + "Architecture" rejoined
        Assert.DoesNotContain(content.Technologies, t => t.Contains("GVHD"));      // the "(GVHD…)" hint is gone
        Assert.DoesNotContain(content.Technologies, t => t.Contains('('));         // no stray parenthetical
        Assert.DoesNotContain("Client", content.Technologies);                     // not split off from "SignalR Client"
        Assert.Equal(content.Technologies.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                     content.Technologies.Count);                                  // no duplicates

        // 3.3 — the expected-outputs prose, with the product-kind checkbox grid trimmed off.
        Assert.Contains("Hệ thống web quản lý", content.ExpectedResults);
        Assert.DoesNotContain("Website application", content.ExpectedResults!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mobile application", content.ExpectedResults!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("🗹", content.ExpectedResults);
        Assert.DoesNotContain("☐", content.ExpectedResults);

        // 3.4 — the feature list, with the signature block trimmed off.
        Assert.Contains("Admin", content.Scope);
        Assert.DoesNotContain("On behalf of Registers", content.Scope!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sign and full name", content.Scope!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Da Nang, 02", content.Scope);

        // Section 1 supervisor — the e-mail must survive both exports (the a1 regression guard).
        Assert.Contains(content.Supervisors, s => s.Email == "quangltn3@fe.edu.vn");

        // Section 2 roster — 5 students, one leader.
        Assert.Equal(5, content.Roster.Count);
        Assert.Contains(content.Roster, r => r is { IsLeader: true, StudentCode: "DE170745" });
    }

    // The bare bug report: a filled PDF once wrongly triggered a1 ("missing mentor") because the
    // supervisor e-mail was not recovered. Keep it as an explicit, named guard.
    [Fact]
    public void Pdf_recovers_supervisor_email_so_a1_does_not_fire()
    {
        var content = Parse("register-form-lecturer.pdf");
        Assert.Contains(content.Supervisors, s => s.Email == "quangltn3@fe.edu.vn");
    }

    // Second bug report: a DOCX whose "Lecturer" tick is a Word content-control checkbox
    // (w14:checkbox with a Wingdings w:sym mark, not a literal glyph) was read as unticked, so the
    // b-rule wrongly fired "Kinds of person make registers cần phải là Lecturer". The reader now reads
    // the checkbox's w14:checked state directly.
    [Fact]
    public void Docx_content_control_checkbox_is_read_as_ticked()
    {
        var content = Parse("register-form-checkbox-sdt.docx");
        Assert.True(content.LecturerRegisterTicked);
    }
}
