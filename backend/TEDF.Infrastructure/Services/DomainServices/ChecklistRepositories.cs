using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Repositories used by <see cref="ChecklistDomainService"/>, bundled into a single injectable
/// dependency so the service constructor stays readable. Registered scoped in DI; every member is
/// resolved by the container exactly as it would be if injected directly.
/// </summary>
public sealed record ChecklistRepositories(
    IProjectRepository Projects,
    IProjectEvaluatorAssignmentRepository Assignments,
    IChecklistConfigRepository Configs,
    IProjectEvaluationChecklistRepository Checklists,
    ISemesterRepository Semesters);
