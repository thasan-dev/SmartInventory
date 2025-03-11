using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

namespace SmartInventory._Framework.DomainModel.DomainEventCreators
{
    public interface IDomainEventCreator
    {
        /// <summary>
        /// Creates to domain event from the creator
        /// </summary>
        /// <returns></returns>
        DomainEvent ToDomainEvent();
    }
}