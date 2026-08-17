using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Services;
using TEDF.Infrastructure.Services.RegisterForm;
using Xunit;

namespace TEDF.Tests;

/// <summary>
/// The propose-time b → a → c rules and the mapping, exercised directly on hand-built content so each
/// branch is covered without a database. Semantics:
/// • a1 fires only when the supervisor block is completely empty; a supervisor with an e-mail but no
///   name (the PDF case) passes a1.
/// • a2 requires the LOGGED-IN lecturer to be the mentor named on the form — the form's supervisor
///   e-mail(s) are matched against the published eligible-mentor list, and the current user's id must be
///   among the matches. The topic's mentor becomes the logged-in lecturer.
/// </summary>
public class RegisterFormProposalValidatorTests
{
    // The logged-in lecturer; also the eligible mentor whose e-mail the happy-path form carries.
    private static readonly Guid MentorId = Guid.NewGuid();
    private static readonly Guid OtherLecturerId = Guid.NewGuid();
    private const string MentorEmail = "quangltn3@fe.edu.vn";

    private static RegisterFormProposalResult Run(
        RegisterFormContent content, IReadOnlyList<EligibleMentor> eligible, Guid? currentUser = null) =>
        RegisterFormProposalValidator.ValidateAndMap(content, eligible, currentUser ?? MentorId);

    private static IReadOnlyList<EligibleMentor> Eligible(Guid? mentorId = null, string? email = MentorEmail) =>
        email is null
            ? new List<EligibleMentor>()
            : [EligibleMentor.Create(1, mentorId ?? MentorId, "EMP001", 1, new EligibleMentorSnapshot(Email: email))];

    private static RegisterFormContent Content(
        bool? lecturer = true,
        IReadOnlyList<RegisterFormSupervisor>? supervisors = null,
        string? nameEn = "Some English Title",
        string? nameVi = "Tên tiếng Việt",
        string? nameAbbr = "TEDF",
        string? objectives = "objectives text",
        IReadOnlyList<string>? technologies = null,
        string? expectedResults = "expected outputs text",
        string? scope = "expected features text") =>
        new(
            Supervisors: supervisors ?? [new RegisterFormSupervisor("Le Thien Nhat Quang", MentorEmail, null)],
            LecturerRegisterTicked: lecturer,
            NameEn: nameEn, NameVi: nameVi, NameAbbr: nameAbbr,
            Description: "brief intro text", Objectives: objectives,
            Technologies: technologies ?? ["React", "Node"],
            ExpectedResults: expectedResults, Scope: scope,
            Roster: []);

    [Fact]
    public void Happy_path_maps_fields_and_sets_current_user_as_mentor()
    {
        var result = Run(Content(), Eligible());

        Assert.Equal("Some English Title", result.NameEn);
        Assert.Equal("TEDF", result.NameAbbr);
        Assert.Equal("React, Node", result.Technologies);
        Assert.Equal([MentorId], result.MentorIds);           // topic mentor = the logged-in lecturer
    }

    [Fact]
    public void B_lecturer_not_ticked_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(lecturer: false), Eligible()));
        Assert.Contains("Kinds of person", ex.Message);
    }

    [Fact]
    public void B_lecturer_unread_is_rejected()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Run(Content(lecturer: null), Eligible()));
    }

    [Fact]
    public void A1_completely_empty_supervisor_asks_to_add_mentor()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(supervisors: []), Eligible()));
        Assert.Contains("bổ sung thông tin về Mentor", ex.Message);
    }

    [Fact]
    public void A1_email_only_supervisor_passes_a1_and_matches() // PDF case: e-mail present, name absent
    {
        var content = Content(supervisors: [new RegisterFormSupervisor(null, MentorEmail, null)]);
        var result = Run(content, Eligible());
        Assert.Equal([MentorId], result.MentorIds);
    }

    [Fact]
    public void A2_supervisor_not_on_published_list_is_rejected()
    {
        var content = Content(supervisors: [new RegisterFormSupervisor("Someone", "notlisted@fe.edu.vn", null)]);
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(content, Eligible()));
        Assert.Contains("không khớp", ex.Message);
    }

    [Fact]
    public void A2_logged_in_lecturer_is_not_the_form_mentor_is_rejected()
    {
        // The exact reported bug: the form's mentor (quangltn3 = MentorId) is a valid published mentor,
        // but a DIFFERENT lecturer is logged in → must be blocked, not silently accepted.
        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            Run(Content(), Eligible(), currentUser: OtherLecturerId));
        Assert.Contains("Tài khoản đăng nhập và mentor của đề tài không khớp nhau", ex.Message);
    }

    [Fact]
    public void A2_email_match_is_case_and_space_insensitive()
    {
        var content = Content(supervisors: [new RegisterFormSupervisor("X", "  QuangLTN3@FE.edu.vn ", null)]);
        var result = Run(content, Eligible());
        Assert.Equal([MentorId], result.MentorIds);
    }

    [Fact]
    public void A2_current_user_matched_among_two_supervisors_passes()
    {
        // Form lists a colleague (quangltn3 = MentorId) and the logged-in lecturer (OtherLecturerId);
        // both are published mentors. The logged-in lecturer is among them → allowed, mentor = them.
        var content = Content(supervisors:
        [
            new RegisterFormSupervisor("Colleague", MentorEmail, null),
            new RegisterFormSupervisor("Me", "lecturer10@fpt.edu.vn", null),
        ]);
        var eligible = new List<EligibleMentor>
        {
            EligibleMentor.Create(1, MentorId, "EMP001", 1, new EligibleMentorSnapshot(Email: MentorEmail)),
            EligibleMentor.Create(1, OtherLecturerId, "EMP010", 1, new EligibleMentorSnapshot(Email: "lecturer10@fpt.edu.vn")),
        };
        var result = Run(content, eligible, currentUser: OtherLecturerId);
        Assert.Equal([OtherLecturerId], result.MentorIds);
    }

    [Theory]
    [InlineData("AB")]      // too short
    [InlineData("ABCDEF")]  // too long
    [InlineData("abms")]    // lowercase
    [InlineData("AB1")]     // contains a digit
    public void C1_invalid_abbreviation_is_rejected(string abbr)
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(nameAbbr: abbr), Eligible()));
        Assert.Contains("3.1", ex.Message);
    }

    [Fact]
    public void C1_missing_english_name_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(nameEn: "   "), Eligible()));
        Assert.Contains("3.1", ex.Message);
    }

    [Fact]
    public void C2_missing_technology_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(technologies: []), Eligible()));
        Assert.Contains("3.2", ex.Message);
    }

    [Fact]
    public void C3_missing_expected_results_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(expectedResults: "  "), Eligible()));
        Assert.Contains("3.3", ex.Message);
    }

    [Fact]
    public void C4_missing_scope_is_rejected()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(Content(scope: "  "), Eligible()));
        Assert.Contains("3.4", ex.Message);
    }

    [Fact]
    public void Order_is_b_then_a_then_c()
    {
        // Everything wrong at once — b (Kinds of person) must be reported first.
        var content = Content(lecturer: false, supervisors: [], nameEn: "  ");
        var ex = Assert.Throws<BusinessRuleValidationException>(() => Run(content, Eligible()));
        Assert.Contains("Kinds of person", ex.Message);
    }
}
