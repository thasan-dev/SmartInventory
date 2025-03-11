using System.Diagnostics.CodeAnalysis;

namespace SmartInventory._Framework.DomainModel.Entities
{
    public abstract class Entity<TEntityId> where TEntityId : EntityId
    {
        /// <summary>
        /// Constructor - used by EntityFramework
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected Entity()
        {
        }
    
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        protected Entity(TEntityId id)
        {
            Id = id;
        }
    
        /// <summary>
        /// The id of the entity.
        /// </summary>
        public TEntityId Id { get; } = null!;

        public abstract object ToDomainEventObject();
    
        public override bool Equals(object? obj)
        {
            if(ReferenceEquals(obj, null))
                return false;
        
            if(obj.GetType() != GetType())
                return false;

            // same instance
            if (ReferenceEquals(this, obj))
                return true;
        
            return EqualityComparer<TEntityId>.Default.Equals(Id, ((Entity<TEntityId>) obj).Id);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TEntityId>.Default.GetHashCode(Id);
        }
    
        public static bool operator ==(Entity<TEntityId> left, Entity<TEntityId> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Entity<TEntityId> left, Entity<TEntityId> right)
        {
            return !(left == right);
        }
    }
}