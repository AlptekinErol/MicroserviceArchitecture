using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;


/// <summary>
/// This interface is heart of the message broker system that we created
/// Subscribe and Unsubscribe methods are taking 2 parameters which is Event and Events Handler Interface
/// and there is constraints for those methods ( it should be T => integration event TH => its handler ) TypeSafe 🎯
/// </summary>
public interface IEventBus : IDisposable
{
    void Publish(IntegrationEvent @event);
    void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}
