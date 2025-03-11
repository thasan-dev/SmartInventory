namespace SmartInventory._Framework.Infra.Out.DomainEventApiProxy;

public interface IDomainEventsPublisher
{
    /// <summary>
    /// Publishes domain event.
    /// </summary>
    Task PublishAsync(CancellationToken cancellationToken);
}