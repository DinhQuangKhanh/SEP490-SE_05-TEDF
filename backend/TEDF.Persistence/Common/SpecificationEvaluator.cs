using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Specifications;

namespace TEDF.Persistence.Common
{
    /// <summary>
    /// Evaluates specifications and builds queries.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public static class SpecificationEvaluator<T> where T : class
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
        {
            var query = inputQuery;

            if (spec.Criteria is not null)
                query = query.Where(spec.Criteria);

            foreach (var include in spec.Includes)
                query = query.Include(include);

            foreach (var includeStr in spec.IncludeStrings)
                query = query.Include(includeStr);

            if (spec.OrderExpressions is { Count: > 0 })
            {
                IOrderedQueryable<T>? ordered = null;
                foreach (var ord in spec.OrderExpressions)
                {
                    switch (ord.OrderType)
                    {
                        case OrderType.OrderBy:
                            ordered = ordered is null
                                ? query.OrderBy(ord.Expression)
                                : ordered.ThenBy(ord.Expression);
                            break;
                        case OrderType.OrderByDescending:
                            ordered = ordered is null
                                ? query.OrderByDescending(ord.Expression)
                                : ordered.ThenByDescending(ord.Expression);
                            break;
                        case OrderType.ThenBy:
                            if (ordered is null)
                                throw new InvalidOperationException("ThenBy cannot be first.");
                            ordered = ordered.ThenBy(ord.Expression);
                            break;
                        case OrderType.ThenByDescending:
                            if (ordered is null)
                                throw new InvalidOperationException("ThenByDescending cannot be first.");
                            ordered = ordered.ThenByDescending(ord.Expression);
                            break;
                    }
                }
                if (ordered is not null)
                    query = ordered;
            }

            if (spec.IsPagingEnabled)
                query = query.Skip(spec.Skip!.Value).Take(spec.Take!.Value);

            return query;
        }
    }
}
