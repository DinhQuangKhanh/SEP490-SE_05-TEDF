namespace TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

/// <summary>
/// Payload for the direct-topic proposal form: the student's own program (derived from the
/// eligible-student roster — the major field is read-only) plus the mentors rostered to supervise
/// that program this upcoming semester.
/// </summary>
public record AvailableMentorsResponse(
    int MajorId,
    string MajorName,
    List<AvailableMentorDto> Mentors
);
