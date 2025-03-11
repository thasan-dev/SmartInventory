using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities
{
    /// <summary>
    /// This represents the id of an entity in the system.
    /// This class could be sub-classed to create concrete entity id classes.
    /// </summary>
    public abstract class EntityId: ValueObject
    {
        public Guid Value { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">The value of an entity id has to be a non-empty guid.</param>
        protected EntityId(Guid value)
        {
            if(value == Guid.Empty)
                throw new InvalidDataException($"{typeof(EntityId)}: Value cannot be an empty Guid.");
        
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}