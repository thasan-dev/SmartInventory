using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

namespace SmartInventory._Framework.DomainModel.DomainEventCreators
{
    public interface IDomainEventCreator<TData> 
        where TData : Entities.DomainEventEntity.ValueObjects.DomainEventData 
    {
        /// <summary>
        /// Creates to domain event from the creator
        /// </summary>
        /// <returns></returns>
        DomainEvent<TData> ToDomainEvent();
    }
}