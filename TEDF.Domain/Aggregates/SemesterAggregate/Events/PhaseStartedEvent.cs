using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Events
{
    public sealed record PhaseStartedEvent(int SemesterId, int PhaseId, SemesterPhaseType PhaseType) : DomainEventBase;
}
