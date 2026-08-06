using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    /// <summary>
    /// Emails every eligible student and every assigned lecturer after the admin publishes a
    /// semester's roster. Enqueued by <c>EnqueueRosterMailsOnRosterPublishedHandler</c>.
    /// </summary>
    /// <remarks>
    /// A roster can hold hundreds of rows, so the recipient list is built here rather than on the
    /// request thread. Each person gets their own document — the roster is never sent as one
    /// message with a shared recipient list.
    /// </remarks>
    public class SendRosterPublishedMailJob
    {
        private readonly ISemestersQueryService _semesters;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly ILogger<SendRosterPublishedMailJob> _logger;

        public SendRosterPublishedMailJob(
            ISemestersQueryService semesters,
            IFirestoreMailQueue mailQueue,
            ILogger<SendRosterPublishedMailJob> logger)
        {
            _semesters = semesters;
            _mailQueue = mailQueue;
            _logger = logger;
        }

        public async Task ExecuteAsync(int semesterId)
        {
            var semester = await _semesters.GetByIdAsync(semesterId);

            // The publish timestamp, not "now": re-running the job must not change the announced date.
            var announcementDate = MailFormat.Date(semester.RosterPublishedAt ?? DateTime.UtcNow);

            var students = await _semesters.GetEligibleStudentsAsync(semesterId);
            var mentors = await _semesters.GetEligibleMentorsAsync(semesterId);

            var messages = new List<TedfMailMessage>();
            messages.AddRange(BuildStudentMessages(students, semester, announcementDate));
            messages.AddRange(BuildLecturerMessages(mentors, semester, announcementDate));

            if (messages.Count == 0)
            {
                _logger.LogInformation("Roster of Semester {SemesterId} has no addressable recipient.", semesterId);
                return;
            }

            var result = await _mailQueue.EnqueueAsync(messages);

            _logger.LogInformation(
                "Roster mail for Semester {SemesterId}: {Queued} queued, {Duplicates} already sent.",
                semesterId, result.Queued, result.Duplicates);
        }

        private IEnumerable<TedfMailMessage> BuildStudentMessages(
            List<EligibleStudentDto> students, SemesterDto semester, string announcementDate)
        {
            var detailUrl = _mailQueue.BuildDetailUrl("/student");

            return students
                .Where(s => s.IsEligible)
                .Select(s => new TedfMailMessage
                {
                    To = s.Email ?? string.Empty,
                    TemplateName = MailTemplateNames.PublishedStudentList,
                    DedupeKey = $"roster-student:{semester.Id}:{s.StudentId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = MailFormat.Text(s.FullName, s.StudentCode),
                        ["semesterName"] = semester.Name,
                        ["announcementDate"] = announcementDate,
                        ["detailUrl"] = detailUrl
                    }
                });
        }

        private IEnumerable<TedfMailMessage> BuildLecturerMessages(
            List<EligibleMentorDto> mentors, SemesterDto semester, string announcementDate)
        {
            var detailUrl = _mailQueue.BuildDetailUrl("/lecturer");

            return mentors
                .Where(m => m.IsAssigned)
                .Select(m => new TedfMailMessage
                {
                    To = m.Email ?? string.Empty,
                    TemplateName = MailTemplateNames.PublishedLecturerList,
                    DedupeKey = $"roster-lecturer:{semester.Id}:{m.MentorId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = MailFormat.Text(m.FullName, m.EmployeeCode),
                        ["semesterName"] = semester.Name,
                        ["announcementDate"] = announcementDate,
                        ["detailUrl"] = detailUrl
                    }
                });
        }
    }
}
