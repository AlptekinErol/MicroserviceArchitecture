using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;

public interface IEventBusSubscriptionManager
{
    event EventHandler<string> OnEventRemoved; // Remove edildiğinde bir event oluşucak ve dışarıdan unsubscribe çalışınca bu event çalışacak
    bool IsEmpty { get; } // her hangi bir event dinleniyor mu?
    void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>; // add subscribe
    void RemoveSubscription<T, TH>() where TH : IIntegrationEventHandler<T> where T : IntegrationEvent; // remove subscribe
    bool HasSubscriptionForEvent<T>() where T : IntegrationEvent; // Dışarıdan event gelince subscribe olup olmadığına bakıcak
    bool HasSubscriptionForEvent(string eventName); // event adına göre bakıcak
    Type GetEventTypeByName(string eventName); // event ismine göre type gelicek
    void Clear(); // tüm subscribelar clear silme
    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent; // eventin tüm subscription ve handlerlarını dönücek
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName); // event adına göre dönücek
    string GetEventKey<T>(); // event isimleri olacak, routing key
}
