using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectRegistration.Queries.GetAvailableMentors;

public record GetAvailableMentorsQuery(int? MajorId = null) : IQuery<List<AvailableMentorDto>>;
