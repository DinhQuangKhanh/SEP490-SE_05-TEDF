using Hangfire;
using System.Linq.Expressions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.BackgroundJobs
{
    public class HangfireJobService : IBackgroundJobService
    {
        public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
            => BackgroundJob.Enqueue(methodCall);

        public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
            => BackgroundJob.Schedule(methodCall, delay);

        public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTime enqueueAt)
            => BackgroundJob.Schedule(methodCall, enqueueAt);

        public void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
            => RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

        public void RemoveRecurring(string jobId)
            => RecurringJob.RemoveIfExists(jobId);
    }
}
