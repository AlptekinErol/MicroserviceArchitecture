using EventBus.Base.Enums;

namespace EventBus.Base;

public class EventBusConfig
{
    public int ConnectionRetryCount { get; set; } = 5;  // RabbitMq bağlanırken max 5 kez dene
    public string DefaultTopicName { get; set; } = "SellingBuddyEventBus";  // Default topic name dışarıdan gelmez ise kullanılır
    public string EventBusConnectionString { get; set; } = String.Empty;
    public string SubscriberClientAppName { get; set; } = String.Empty; // Hangi servis yeni queue yaratacak (order servis? basketservice?) NotificationService.OrderCreated mesela
    public string EventNamePrefix { get; set; } = String.Empty; // Prefix ve Suffix kullanımı => OrderCreatedIntegrationEvent yerine OrderCreated görmek için
    public string EventNameSuffix { get; set; } = "IntegrationEvent"; // 11.
    public EventBusType EventBusType { get; set; } = EventBusType.RabbitMq; // Default EventBus
    public object Connection { get; set; }  // Başka bir messageBroker kullanımı için ileriki durumlarda. Object Factory? connection
    public bool DeleteEventPrefix => !String.IsNullOrEmpty(EventNamePrefix);
    public bool DeleteEventSuffix => !String.IsNullOrEmpty(EventNameSuffix);
}