namespace SmartInventory._Framework.Infra.Out.DomainEventApiProxy;

public abstract class DomainEventsPublisher: IDomainEventsPublisher
{
    public Task PublishAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}