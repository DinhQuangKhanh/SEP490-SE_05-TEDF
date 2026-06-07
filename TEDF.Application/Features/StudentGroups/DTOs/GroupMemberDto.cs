namespace TEDF.Application.Features.StudentGroups.DTOs
{
    public record GroupMemberDto
    {
        public Guid StudentId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? StudentCode { get; init; }
        public string? Email { get; init; }
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime JoinedAt { get; init; }
    }
}
