using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectRegistration.Commands.UpdateDirectTopic;

public sealed record UpdateDirectTopicCommand(
    Guid ProjectId,
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
) : ICacheInvalidatingCommand
{
  public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
  [
      $"topics:detail:{ProjectId}",
        "topics:list:",
        "topic-pools:"
  ];
}
