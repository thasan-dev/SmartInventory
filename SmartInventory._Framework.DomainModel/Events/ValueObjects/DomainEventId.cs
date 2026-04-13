namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    public class DomainEventId: EntityId
    {
        /// <summary>
        /// A unique id for a domain event
        /// </summary>
        private DomainEventId(Guid value) : base(value)
        {
        }
    
        public static DomainEventId Create(Guid value)
        {
            return new DomainEventId(value);
        }
    }
}