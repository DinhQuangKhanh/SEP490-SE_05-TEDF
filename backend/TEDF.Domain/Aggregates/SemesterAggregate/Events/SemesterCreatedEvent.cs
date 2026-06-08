using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Events
{
    public sealed record SemesterCreatedEvent(int SemesterId, string SemesterCode) : DomainEventBase;
}
