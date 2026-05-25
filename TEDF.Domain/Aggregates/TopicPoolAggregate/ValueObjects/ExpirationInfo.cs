using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.ValueObjects
{
    public sealed class ExpirationInfo : ValueObject
    {
        public int CreatedSemesterId { get; }
        public int ExpirationSemesterId { get; }
        public int SemestersUntilExpiration { get; }

        private ExpirationInfo(int createdSemesterId, int expirationSemesterId, int semestersUntilExpiration)
        {
            CreatedSemesterId = createdSemesterId;
            ExpirationSemesterId = expirationSemesterId;
            SemestersUntilExpiration = semestersUntilExpiration;
        }

        public static ExpirationInfo Create(int createdSemesterId, int expirationSemesterId)
        {
            var semestersUntilExpiration = expirationSemesterId - createdSemesterId;
            return new ExpirationInfo(createdSemesterId, expirationSemesterId, semestersUntilExpiration);
        }

        public bool IsExpired(int currentSemesterId) => currentSemesterId > ExpirationSemesterId;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return CreatedSemesterId;
            yield return ExpirationSemesterId;
        }
    }
}
