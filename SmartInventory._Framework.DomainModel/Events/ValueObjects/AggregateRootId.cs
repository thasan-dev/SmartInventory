namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    public class AggregateRootId: EntityId
    {
        /// <summary>
        /// A unique id for a aggregateRoot
        /// </summary>
        private AggregateRootId(Guid value) : base(value)
        {
        }
    
        public static AggregateRootId Create(Guid value)
        {
            return new AggregateRootId(value);
        }
    }
}