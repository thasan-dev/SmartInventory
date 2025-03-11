using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    public class MicroserviceName : ValueObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">The microservice name.</param>
        public MicroserviceName(string value)
        {
            Value = value;
        }
    
        /// <summary>
        /// The microservice name.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// List of properties to include in equal comparison.
        /// </summary>
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}