using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Domain.Aggregates.SupportAggregate
{
    public interface ISupportTicketRepository : IRepository<SupportTicket, Guid>
    {
        Task<SupportTicket?> GetByCodeAsync(TicketCode code, CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetByReporterIdAsync(Guid reporterId, CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetByCategoryAsync(TicketCategory category, CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetOpenAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<SupportTicket>> GetUnassignedAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsCodeAsync(TicketCode code, CancellationToken cancellationToken = default);
        Task<int> GetNextSequenceAsync(int year, CancellationToken cancellationToken = default);
        Task<Dictionary<TicketStatus, int>> GetStatusCountAsync(CancellationToken cancellationToken = default);
    }
}
