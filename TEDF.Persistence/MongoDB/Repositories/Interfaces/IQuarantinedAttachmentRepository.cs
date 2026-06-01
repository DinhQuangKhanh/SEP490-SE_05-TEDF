using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces;

public interface IQuarantinedAttachmentRepository
{
    Task AddAsync(QuarantinedAttachmentDocument document, CancellationToken ct = default);
    Task DeleteByQuarantinePathAsync(string quarantinePath, CancellationToken ct = default);
    Task<IReadOnlyList<QuarantinedAttachmentDocument>> GetStaleAsync(TimeSpan olderThan, CancellationToken ct = default);
    Task<QuarantinedAttachmentDocument?> GetByQuarantinePathAsync(string quarantinePath, CancellationToken ct = default);
    Task SetCleanPathAsync(string quarantinePath, string cleanPath, CancellationToken ct = default);
}
