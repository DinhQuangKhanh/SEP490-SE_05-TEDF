namespace TEDF.Domain.Common.Primitives
{
    public interface IIdentifiable<TId> where TId : notnull
    {
        TId Id { get; }
    }
}
