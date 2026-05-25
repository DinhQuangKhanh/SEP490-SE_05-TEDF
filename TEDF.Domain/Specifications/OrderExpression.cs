using System.Linq.Expressions;

namespace TEDF.Domain.Specifications
{
    public enum OrderType
    {
        OrderBy,
        OrderByDescending,
        ThenBy,
        ThenByDescending
    }

    public sealed record OrderExpression<T>(
        Expression<Func<T, object>> Expression,
        OrderType OrderType
    );
}
