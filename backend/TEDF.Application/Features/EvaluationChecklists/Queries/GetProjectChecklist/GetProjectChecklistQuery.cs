using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectChecklist;

/// <summary>Loads the current evaluator's checklist state for a project (applicable criteria + saved results).</summary>
public record GetProjectChecklistQuery(Guid ProjectId) : IQuery<ProjectChecklistDto>;
