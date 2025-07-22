using System.Diagnostics.CodeAnalysis;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Aggregates
{
    public abstract class AggregateRoot<TId,TDomainEvent>: Entity<TId> where TId : EntityId where TDomainEvent :DomainEvent
    {
        /// <summary>
        /// Constructor - used by EntityFramework
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected AggregateRoot()
        {
        }
    
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">The unique id for this instance.</param>
        protected AggregateRoot(TId id)
            : base(id)
        { }
    
        public TDomainEvent DomainEvent { get; protected set; } = null!;

        protected abstract void RaiseDomainEvent<TAggregateRoot>(DomainEventType domainEventType)
            where TAggregateRoot : AggregateRoot<TId, TDomainEvent>;
    }
}