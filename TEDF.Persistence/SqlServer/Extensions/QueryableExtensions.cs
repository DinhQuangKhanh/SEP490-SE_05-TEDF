using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace TEDF.Persistence.SqlServer.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> IncludeIf<T, TProperty>(this IQueryable<T> source, bool condition, Expression<Func<T, TProperty>> path) where T : class
            => condition ? source.Include(path) : source;

        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
            => condition ? source.Where(predicate) : source;
    }
}
