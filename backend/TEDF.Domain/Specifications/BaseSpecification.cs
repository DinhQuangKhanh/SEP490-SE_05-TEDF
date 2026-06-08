using System.Linq.Expressions;

namespace TEDF.Domain.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; private set; } = _ => true;
        public List<Expression<Func<T, object>>> Includes { get; } = [];
        public List<string> IncludeStrings { get; } = [];
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public List<OrderExpression<T>> OrderExpressions { get; } = [];
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        protected BaseSpecification() { }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
        protected void ApplyOrderBy(Expression<Func<T, object>> expression)
        => OrderExpressions.Add(new(expression, OrderType.OrderBy));

        protected void ApplyOrderByDescending(Expression<Func<T, object>> expression)
            => OrderExpressions.Add(new(expression, OrderType.OrderByDescending));

        protected void ApplyThenBy(Expression<Func<T, object>> expression)
            => OrderExpressions.Add(new(expression, OrderType.ThenBy));

        protected void ApplyThenByDescending(Expression<Func<T, object>> expression)
            => OrderExpressions.Add(new(expression, OrderType.ThenByDescending));

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
        public static IQueryable<T> ApplyOrdering(
            IQueryable<T> query,
            ISpecification<T> spec)
        {
            if (spec.OrderExpressions.Count == 0)
                return query;

            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var order in spec.OrderExpressions)
            {
                if (orderedQuery is null)
                {
                    orderedQuery = order.OrderType switch
                    {
                        OrderType.OrderBy =>
                            query.OrderBy(order.Expression),
                        OrderType.OrderByDescending =>
                            query.OrderByDescending(order.Expression),
                        _ => throw new InvalidOperationException(
                            "First order must be OrderBy or OrderByDescending")
                    };
                }
                else
                {
                    orderedQuery = order.OrderType switch
                    {
                        OrderType.ThenBy =>
                            orderedQuery.ThenBy(order.Expression),
                        OrderType.ThenByDescending =>
                            orderedQuery.ThenByDescending(order.Expression),
                        _ => orderedQuery
                    };
                }
            }

            return orderedQuery ?? query;
        }
    }
}
