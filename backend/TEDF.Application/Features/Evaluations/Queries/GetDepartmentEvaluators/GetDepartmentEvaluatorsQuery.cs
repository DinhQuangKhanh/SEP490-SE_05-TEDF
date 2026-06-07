using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetDepartmentEvaluators;

public record GetDepartmentEvaluatorsQuery : IQuery<List<DepartmentEvaluatorDto>>;
