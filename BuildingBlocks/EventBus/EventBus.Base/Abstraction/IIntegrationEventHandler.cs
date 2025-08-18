using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;

/// <summary>
/// Event Handler interface, i make this interface to must use IIntegrationEvent 
/// =>
/// every event should have handler
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
}