using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigs;

/// <summary>Lists checklist configurations (optionally filtered by semester) plus the semester options.</summary>
public record GetChecklistConfigsQuery(int? SemesterId) : IQuery<ChecklistConfigListDto>;
