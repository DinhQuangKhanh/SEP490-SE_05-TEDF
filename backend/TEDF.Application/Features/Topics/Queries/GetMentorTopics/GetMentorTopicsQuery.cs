using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Topics.DTOs;

namespace TEDF.Application.Features.Topics.Queries.GetMentorTopics;

public record GetMentorTopicsQuery(
    int? SemesterId,
    string? Search,
    int Page = 1,
    int PageSize = 10
) : IQuery<GetMentorTopicsResult>;
