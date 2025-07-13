using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider ServiceProvider;
    public readonly IEventBusSubscriptionManager SubsManager;

    private EventBusConfig eventBusConfig;

    public BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
    {
        eventBusConfig = config;
        ServiceProvider = serviceProvider;
        SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
    }

    /// <summary>
    /// Removing IntegrationEvent keyword example: IntegrationEventOrderCreated 
    /// => this will trim for prefix or suffix
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public virtual string ProcessEventName(string eventName)
    {
        if (eventBusConfig.DeleteEventPrefix)
        {
            eventName = eventName.TrimStart(eventBusConfig.EventNamePrefix.ToArray());
        }

        if (eventBusConfig.DeleteEventSuffix)
        {
            eventName = eventName.TrimEnd(eventBusConfig.EventNameSuffix.ToArray());
        }

        return eventName;
    }

    /// <summary>
    /// returning refactored queue name with ProcessEventName method
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public virtual string GetSubName(string eventName)
    {
        return $"{eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        eventBusConfig = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> ProcessEvent(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);

        var processed = false;

        if (SubsManager.HasSubscriptionForEvent(eventName))  // eventName dinleniyor mu kontrol
        {
            var subscriptions = SubsManager.GetHandlersForEvent(eventName);  // tüm subscriptionları ver

            using (var scope = ServiceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null) continue;

                    var eventType = SubsManager.GetEventTypeByName($"{eventBusConfig.EventNamePrefix}{eventName}{eventBusConfig.EventNameSuffix}"); // kırpılmamış event'in type'ı elde ediliyor
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);


                    //reflection ? 
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                }
            }

            processed = true;
        }

        return processed;
    }

    // publish subscribe ve unsubscribe kullanılan message broker'lara göre özel olacak
    public abstract void Publish(IntegrationEvent @event);
    public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}
