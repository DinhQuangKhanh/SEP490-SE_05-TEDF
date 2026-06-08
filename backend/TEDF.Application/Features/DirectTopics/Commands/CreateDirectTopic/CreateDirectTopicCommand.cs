using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Commands.CreateDirectTopic;

public record CreateDirectTopicCommand(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    Guid MentorId,
    Guid GroupId,
    int MajorId,
    int MaxStudents
) : ICommand<Guid>;
