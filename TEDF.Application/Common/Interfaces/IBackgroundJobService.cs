using System.Linq.Expressions;

namespace TEDF.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTime enqueueAt);
    void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);
    void RemoveRecurring(string jobId);
}
