using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetEligibleMentors;

/// <summary>Returns the eligible-mentor roster for a semester (admin preview table).</summary>
public record GetEligibleMentorsQuery(int SemesterId) : IQuery<List<EligibleMentorDto>>;
