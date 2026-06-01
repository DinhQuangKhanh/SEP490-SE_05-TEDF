namespace TEDF.Infrastructure.BackgroundJobs.Scheduling
{
    /// <summary>
    /// Interface for job scheduling.
    /// </summary>
    public interface IJobScheduler
    {
        string Enqueue<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall);
        string Enqueue<T>(System.Linq.Expressions.Expression<Action<T>> methodCall);
        string Schedule<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay);
        string Schedule<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);
        void AddOrUpdateRecurring<T>(string recurringJobId, System.Linq.Expressions.Expression<Func<T, Task>> methodCall, string cronExpression, TimeZoneInfo? timeZone = null);
        void AddOrUpdateRecurring<T>(string recurringJobId, System.Linq.Expressions.Expression<Action<T>> methodCall, string cronExpression, TimeZoneInfo? timeZone = null);
        void RemoveRecurring(string recurringJobId);
        void TriggerRecurring(string recurringJobId);
        string ContinueWith<T>(string parentJobId, System.Linq.Expressions.Expression<Func<T, Task>> methodCall);
        bool Delete(string jobId);
        bool Requeue(string jobId);
    }
}
