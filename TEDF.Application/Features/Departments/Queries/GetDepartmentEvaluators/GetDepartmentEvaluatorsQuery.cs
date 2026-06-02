using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Departments.DTOs;

namespace TEDF.Application.Features.Departments.Queries.GetDepartmentEvaluators;

public record GetDepartmentEvaluatorsQuery : IQuery<List<DepartmentEvaluatorDto>>;
