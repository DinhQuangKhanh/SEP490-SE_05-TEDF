using Hangfire;
using TEDF.Infrastructure.BackgroundJobs.Jobs;

namespace TEDF.Infrastructure.BackgroundJobs.Scheduling
{
    public static class RecurringJobsConfiguration
    {
        public static void ConfigureRecurringJobs()
        {
            // Topic expiration check - daily at 1 AM
            RecurringJob.AddOrUpdate<TopicExpirationJob>(
                "topic-expiration",
                job => job.ExecuteAsync(),
                Cron.Daily(1));

            // Evaluation reminders - every 4 hours
            RecurringJob.AddOrUpdate<EvaluationReminderJob>(
                "evaluation-reminder",
                job => job.ExecuteAsync(),
                "0 */4 * * *");

            // Semester phase transition - daily at midnight
            RecurringJob.AddOrUpdate<SemesterPhaseTransitionJob>(
                "semester-phase-transition",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily());

            // Defense schedule reminders - daily at 8 AM
            RecurringJob.AddOrUpdate<DefenseScheduleReminderJob>(
                "defense-reminder",
                job => job.ExecuteAsync(),
                Cron.Daily(8));

            // Meeting reminders - every hour
            RecurringJob.AddOrUpdate<MeetingReminderJob>(
                "meeting-reminder",
                job => job.ExecuteAsync(),
                Cron.Hourly);

            // Join request expiration - every 5 minutes
            RecurringJob.AddOrUpdate<GroupJoinRequestExpirationJob>(
                "group-join-request-expiration",
                job => job.ExecuteAsync(),
                "*/5 * * * *");

            // Data cleanup - weekly on Sunday at 2 AM
            RecurringJob.AddOrUpdate<DataCleanupJob>(
                "data-cleanup",
                job => job.ExecuteAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 2));

            // Removed: mentor access is no longer revoked at semester end (lecturers keep the Mentor role for
            // login + proposing topics; supervising/evaluation is gated per-semester via the eligible-mentor list).
            RecurringJob.RemoveIfExists("mentor-access-expiry");
        }
    }
}
