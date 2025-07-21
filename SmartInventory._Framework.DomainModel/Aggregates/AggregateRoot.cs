using System.Diagnostics.CodeAnalysis;
using SmartInventory._Framework.DomainModel.DomainEventCreators;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Aggregates
{
    public abstract class AggregateRoot<TId,TData>: Entity<TId> where TId : EntityId where TData : DomainEventData
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
    
        public DomainEvent<TData> DomainEvent { get; private set; } = null!;

        protected void HandleDomainCommand(Func<IDomainEventCreator<TData>> commandHandler)
        {
            DomainEvent = commandHandler.Invoke().ToDomainEvent();
        }
    }
}