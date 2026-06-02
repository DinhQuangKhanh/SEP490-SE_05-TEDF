using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Common.Primitives
{
    public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        protected AggregateRoot(TId id) : base(id) { }

        protected AggregateRoot() { }

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// Checks if a business rule is satisfied. Throws BusinessRuleValidationException if the rule is broken.
        /// </summary>
        /// <param name="rule">The business rule to check.</param>
        /// <exception cref="BusinessRuleValidationException">Thrown when the rule is broken.</exception>
        protected void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleValidationException(rule);
            }
        }

        /// <summary>
        /// Checks multiple business rules. Throws an exception for the first broken rule.
        /// </summary>
        /// <param name="rules">The business rules to check.</param>
        /// <exception cref="BusinessRuleValidationException">Thrown when any rule is broken.</exception>
        protected void CheckRules(params IBusinessRule[] rules)
        {
            foreach (var rule in rules)
            {
                CheckRule(rule);
            }
        }
    }
}
