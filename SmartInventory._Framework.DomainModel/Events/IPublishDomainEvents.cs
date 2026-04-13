namespace SmartInventory._Framework.DomainModel.Events;

public interface IPublishDomainEvents<TPayload> where TPayload : class
{
    TPayload GetEventPayload();
}
