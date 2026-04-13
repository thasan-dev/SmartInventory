using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    /// <summary>
    /// The name of the domain event
    /// </summary>
    public class DomainEventName: ValueObject
    {
        private DomainEventName(string value)
        {
            Value = value;
        }
        public string Value { get; }
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    
        public static DomainEventName Create(string value)
        {
            return new DomainEventName(value);
        }
    }
}