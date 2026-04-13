using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    public class IsPublished : ValueObject
    {
        /// <summary>
        /// The microservice name.
        /// </summary>
        public bool Value { get; }
    
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">The microservice name.</param>
        public IsPublished(bool value)
        {
            Value = value;
        }
    
        /// <summary>
        /// List of properties to include in equal comparison.
        /// </summary>
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
        
        public static IsPublished Create(bool value)
        {
            return new IsPublished(value);
        }
    }
}