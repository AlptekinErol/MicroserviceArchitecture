using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Enums;
using EventBus.RabbitMq;

namespace EventBus.Factory;

public class EventBusFactory
{
    public static IEventBus Create(EventBusConfig config, IServiceProvider serviceProvider)
    {
        // var conn = New DefaultServiceBusPersisterConnection(config);

        return config.EventBusType switch
        {
            EventBusType.AzureServiceBus => new EventBusServiceBus(config, serviceProvider),
            _ => new EventBusRabbitMQ(config, serviceProvider),
        };
    }
}
