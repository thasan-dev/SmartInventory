using System.Diagnostics.CodeAnalysis;
using SmartInventory._Framework.DomainModel.DomainEventCreators;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

namespace SmartInventory._Framework.DomainModel.Aggregates
{
    public abstract class AggregateRoot<TId>: Entity<TId> where TId : EntityId
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
    
        public DomainEvent DomainEvent { get; private set; } = null!;

        protected void Handle(Func<IDomainEventCreator> commandHandler)
        {
            DomainEvent = commandHandler.Invoke().ToDomainEvent();
        }
    }
}