using TEDF.Domain.Enums.Ticket;

namespace TEDF.API.Endpoints.SupportTickets.Requests;

public record GetTicketsRequest(string? SearchTerm, TicketStatus? Status, TicketPriority? Priority);
public record CreateTicketRequest(string Title, string Description, TicketCategory Category, TicketPriority Priority);
public record ReplyTicketRequest(string Content);
public record UpdateTicketStatusRequest(TicketStatus Status);
