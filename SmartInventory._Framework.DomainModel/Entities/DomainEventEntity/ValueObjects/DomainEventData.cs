using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects
{
    /// <summary>
    /// The Json data of the domain event
    /// </summary>
    public class DomainEventData: ValueObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataAsJson">The event data as json.</param>
        private DomainEventData(string dataAsJson)
        {

            DataAsJson = dataAsJson;
        }
    
        /// <summary>
        /// Create a new instance of DomainEventData
        /// </summary>
        public static DomainEventData Create(string dataAsJson)
        {
            return new DomainEventData(dataAsJson);
        }
        /// <summary>
        /// The domain event data as json.
        /// </summary>
        public string DataAsJson { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return DataAsJson;
        }
    }
}