using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigById;

/// <summary>Loads a single checklist configuration with its criteria.</summary>
public record GetChecklistConfigByIdQuery(Guid Id) : IQuery<ChecklistConfigDto?>;
