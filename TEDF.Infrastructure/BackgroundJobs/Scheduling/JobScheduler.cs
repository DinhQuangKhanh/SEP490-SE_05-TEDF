using Hangfire;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace TEDF.Infrastructure.BackgroundJobs.Scheduling
{
    /// <summary>
    /// Service for scheduling and managing background jobs.
    /// </summary>
    public class JobScheduler : IJobScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<JobScheduler> _logger;

        public JobScheduler(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<JobScheduler> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _logger = logger;
        }

        #region Fire-and-forget jobs

        /// <summary>
        /// Enqueues a job to be executed immediately.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="methodCall">The method to execute.</param>
        /// <returns>The job ID.</returns>
        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        {
            var jobId = _backgroundJobClient.Enqueue(methodCall);
            _logger.LogInformation("Job enqueued: {JobId}, Type: {JobType}", jobId, typeof(T).Name);
            return jobId;
        }

        /// <summary>
        /// Enqueues a job to be executed immediately.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="methodCall">The method to execute.</param>
        /// <returns>The job ID.</returns>
        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            var jobId = _backgroundJobClient.Enqueue(methodCall);
            _logger.LogInformation("Job enqueued: {JobId}, Type: {JobType}", jobId, typeof(T).Name);
            return jobId;
        }

        #endregion

        #region Scheduled jobs

        /// <summary>
        /// Schedules a job to be executed after a delay.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="methodCall">The method to execute.</param>
        /// <param name="delay">The delay before execution.</param>
        /// <returns>The job ID.</returns>
        public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        {
            var jobId = _backgroundJobClient.Schedule(methodCall, delay);
            _logger.LogInformation("Job scheduled: {JobId}, Type: {JobType}, Delay: {Delay}",
                jobId, typeof(T).Name, delay);
            return jobId;
        }

        /// <summary>
        /// Schedules a job to be executed at a specific time.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="methodCall">The method to execute.</param>
        /// <param name="enqueueAt">The time to execute.</param>
        /// <returns>The job ID.</returns>
        public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
        {
            var jobId = _backgroundJobClient.Schedule(methodCall, enqueueAt);
            _logger.LogInformation("Job scheduled: {JobId}, Type: {JobType}, At: {EnqueueAt}",
                jobId, typeof(T).Name, enqueueAt);
            return jobId;
        }

        #endregion

        #region Recurring jobs

        /// <summary>
        /// Adds or updates a recurring job.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="recurringJobId">The unique recurring job ID.</param>
        /// <param name="methodCall">The method to execute.</param>
        /// <param name="cronExpression">The cron expression for scheduling.</param>
        /// <param name="timeZone">Optional time zone.</param>
        public void AddOrUpdateRecurring<T>(
            string recurringJobId,
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo? timeZone = null)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = timeZone ?? TimeZoneInfo.Local
            };

            _recurringJobManager.AddOrUpdate(recurringJobId, methodCall, cronExpression, options);
            _logger.LogInformation("Recurring job added/updated: {JobId}, Type: {JobType}, Cron: {Cron}",
                recurringJobId, typeof(T).Name, cronExpression);
        }

        /// <summary>
        /// Adds or updates a recurring job.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="recurringJobId">The unique recurring job ID.</param>
        /// <param name="methodCall">The method to execute.</param>
        /// <param name="cronExpression">The cron expression for scheduling.</param>
        /// <param name="timeZone">Optional time zone.</param>
        public void AddOrUpdateRecurring<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo? timeZone = null)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = timeZone ?? TimeZoneInfo.Local
            };

            _recurringJobManager.AddOrUpdate(recurringJobId, methodCall, cronExpression, options);
            _logger.LogInformation("Recurring job added/updated: {JobId}, Type: {JobType}, Cron: {Cron}",
                recurringJobId, typeof(T).Name, cronExpression);
        }

        /// <summary>
        /// Removes a recurring job.
        /// </summary>
        /// <param name="recurringJobId">The recurring job ID to remove.</param>
        public void RemoveRecurring(string recurringJobId)
        {
            _recurringJobManager.RemoveIfExists(recurringJobId);
            _logger.LogInformation("Recurring job removed: {JobId}", recurringJobId);
        }

        /// <summary>
        /// Triggers a recurring job immediately.
        /// </summary>
        /// <param name="recurringJobId">The recurring job ID to trigger.</param>
        public void TriggerRecurring(string recurringJobId)
        {
            _recurringJobManager.Trigger(recurringJobId);
            _logger.LogInformation("Recurring job triggered: {JobId}", recurringJobId);
        }

        #endregion

        #region Continuation jobs

        /// <summary>
        /// Creates a continuation job that runs after another job completes.
        /// </summary>
        /// <typeparam name="T">The job type.</typeparam>
        /// <param name="parentJobId">The parent job ID.</param>
        /// <param name="methodCall">The method to execute.</param>
        /// <returns>The continuation job ID.</returns>
        public string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall)
        {
            var jobId = _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
            _logger.LogInformation("Continuation job created: {JobId}, Parent: {ParentJobId}, Type: {JobType}",
                jobId, parentJobId, typeof(T).Name);
            return jobId;
        }

        #endregion

        #region Job management

        /// <summary>
        /// Deletes a job.
        /// </summary>
        /// <param name="jobId">The job ID to delete.</param>
        /// <returns>True if deleted successfully.</returns>
        public bool Delete(string jobId)
        {
            var result = _backgroundJobClient.Delete(jobId);
            if (result)
            {
                _logger.LogInformation("Job deleted: {JobId}", jobId);
            }
            return result;
        }

        /// <summary>
        /// Requeues a failed job.
        /// </summary>
        /// <param name="jobId">The job ID to requeue.</param>
        /// <returns>True if requeued successfully.</returns>
        public bool Requeue(string jobId)
        {
            var result = _backgroundJobClient.Requeue(jobId);
            if (result)
            {
                _logger.LogInformation("Job requeued: {JobId}", jobId);
            }
            return result;
        }

        #endregion
    }
}
