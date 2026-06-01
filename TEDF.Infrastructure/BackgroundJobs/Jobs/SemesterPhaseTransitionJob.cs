using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class SemesterPhaseTransitionJob
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SemesterPhaseTransitionJob> _logger;

        public SemesterPhaseTransitionJob(
            ISemesterRepository semesterRepository,
            IUnitOfWork unitOfWork,
            ILogger<SemesterPhaseTransitionJob> logger)
        {
            _semesterRepository = semesterRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting SemesterPhaseTransitionJob - checking for upcoming Registration phases.");

            // Find all semesters where ANY phase starts in exactly 3 days
            var upcomingSemesters = await _semesterRepository.GetSemestersWithPhaseStartingInAsync(3, cancellationToken);
            var count = 0;

            foreach (var semester in upcomingSemesters)
            {
                // We are looking for Registration phase that starts in 3 days
                var targetDate = DateTime.UtcNow.Date.AddDays(3);
                var registrationPhase = semester.Phases.FirstOrDefault(p => 
                    p.Type == SemesterPhaseType.Registration && p.StartDate.Date == targetDate);

                if (registrationPhase != null)
                {
                    _logger.LogInformation("Found upcoming Registration phase {PhaseId} in Semester {SemesterId}", registrationPhase.Id, semester.Id);
                    
                    // Trigger the Domain Event
                    semester.NotifyUpcomingPhase(registrationPhase.Id);
                    count++;
                }
            }

            if (count > 0)
            {
                // Save changes will trigger the DomainEventInterceptor, which will dispatch PhaseUpcomingEvent to MediatR
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Dispatched {Count} PhaseUpcomingEvents for upcoming Registration phases.", count);
            }

            _logger.LogInformation("SemesterPhaseTransitionJob completed");
        }
    }
}
