namespace TEDF.Application.Features.Supports.DTOs;

public record UserBriefDto(Guid Id, string FullName, string Email, string Role);

public record TicketMessageDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public UserBriefDto? Sender { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record TicketDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    
    public UserBriefDto Reporter { get; init; } = null!;
    public UserBriefDto? Assignee { get; init; }

    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }

    public List<TicketMessageDto> Messages { get; init; } = new();
}

public record TicketListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public UserBriefDto Reporter { get; init; } = null!;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record TicketStatsDto
{
    public int TotalTickets { get; init; }
    public int Unread { get; init; }
    public int InProgress { get; init; }
    public int Resolved { get; init; }
}
