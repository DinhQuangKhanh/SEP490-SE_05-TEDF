using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Enums.Ticket;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for SupportTicket aggregate.
    /// </summary>
    public class SupportTicketRepository : BaseRepository<SupportTicket, Guid>, ISupportTicketRepository
    {
        public SupportTicketRepository(AppDbContext context) : base(context) { }

        public override async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<SupportTicket?> GetByCodeAsync(TicketCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetByReporterIdAsync(Guid reporterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.ReporterId == reporterId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.AssigneeId == assigneeId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetByCategoryAsync(TicketCategory category, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.Category == category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetOpenAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SupportTicket>> GetUnassignedAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.AssigneeId == null && t.Status == TicketStatus.Open)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(TicketCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(t => t.Code == code, cancellationToken);
        }

        public async Task<int> GetNextSequenceAsync(int year, CancellationToken cancellationToken = default)
        {
            var prefix = $"TK-{year}-";
            var lastCode = await _dbSet
                .Where(t => t.CreatedAt.Year == year)
                .OrderByDescending(t => t.Code)
                .Select(t => t.Code)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastCode == null) return 1;

            var sequencePart = lastCode.Value.Replace(prefix, "");
            return int.TryParse(sequencePart, out var seq) ? seq + 1 : 1;
        }

        /// <summary>
        /// Gets high priority open tickets.
        /// </summary>
        public async Task<IEnumerable<SupportTicket>> GetHighPriorityOpenAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
                           (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Urgent))
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<TicketStatus, int>> GetStatusCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }
    }
}
