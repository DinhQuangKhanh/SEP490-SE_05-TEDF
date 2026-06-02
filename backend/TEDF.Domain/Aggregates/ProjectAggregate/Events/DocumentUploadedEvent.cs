using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Document;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Events
{
    public sealed record DocumentUploadedEvent(Guid ProjectId, Guid DocumentId, DocumentType DocumentType) : DomainEventBase;
}
