using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Infrastructure.Services.Email;
using TEDF.Infrastructure.Services.Email.Templates;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    /// <summary>
    /// Sends the "you are eligible for graduation projects" email to every eligible student of a semester.
    /// Enqueued by <c>EnqueueStudentEmailsOnRosterPublishedHandler</c> when the roster is published.
    /// </summary>
    public class SendEligibleStudentEmailsJob
    {
        private readonly ISemestersQueryService _semesters;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<SendEligibleStudentEmailsJob> _logger;

        public SendEligibleStudentEmailsJob(
            ISemestersQueryService semesters,
            IEmailService emailService,
            IEmailTemplateService templateService,
            ILogger<SendEligibleStudentEmailsJob> logger)
        {
            _semesters = semesters;
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
        }

        public async Task ExecuteAsync(int semesterId)
        {
            var students = await _semesters.GetEligibleStudentsAsync(semesterId);
            var recipients = students
                .Where(s => s.IsEligible && !string.IsNullOrWhiteSpace(s.Email))
                .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogInformation("No eligible-student emails to send for Semester {SemesterId}.", semesterId);
                return;
            }

            var semester = await _semesters.GetByIdAsync(semesterId);
            const string subject = "TEDF — Bạn đủ điều kiện làm đồ án tốt nghiệp";

            var messages = recipients.Select(s =>
            {
                var body = _templateService.RenderTemplate(
                    EmailTemplates.EligibleStudentNotice,
                    new EligibleStudentNoticeModel(s.FullName ?? "bạn", semester.Name));
                return new EmailMessage(s.Email!, subject, body);
            }).ToList();

            var result = await _emailService.SendBulkAsync(messages);

            _logger.LogInformation(
                "Eligible-student emails for Semester {SemesterId}: {Sent} sent, {Failed} failed.",
                semesterId, result.TotalSent, result.TotalFailed);
        }

        /// <summary>Render model for <see cref="EmailTemplates.EligibleStudentNotice"/>.</summary>
        public record EligibleStudentNoticeModel(string RecipientName, string SemesterName);
    }
}
