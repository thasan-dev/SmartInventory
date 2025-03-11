using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    /// <summary>
    /// The name of the domain event
    /// </summary>
    public class AggregateRootName: ValueObject
    {
        private AggregateRootName(string value)
        {
            Value = value;
        }
        public string Value { get; }
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    
        public static AggregateRootName Create(string value)
        {
            return new AggregateRootName(value);
        }
    }
}