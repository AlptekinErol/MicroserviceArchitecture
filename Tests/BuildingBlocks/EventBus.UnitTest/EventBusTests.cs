using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Enums;
using EventBus.Factory;
using EventBus.UnitTest.Events.EventHandlers;
using EventBus.UnitTest.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.UnitTest;

public class EventBusTests
{

    private ServiceCollection services;

    public EventBusTests()
    {
        services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
    }

    /// <summary>
    /// RabbitMq and AzureServiceBus Testing Queue, Exchange and Topic Creationg
    /// </summary>
    [Fact]
    public void Subscribe_Event_On_RabbitMq_Test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });

        var sp = services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

        eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }

    [Fact]
    public void Send_Message_To_RabbitMq_Test()
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });

        var sp = services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Publish(new OrderCreatedIntegrationEvent(1));
    }

    /// <summary>
    /// RabbitMQ Configuration for EventBus
    /// </summary>
    /// <returns></returns>
    private EventBusConfig GetRabbitMQConfig()
    {
        return new EventBusConfig
        {
            ConnectionRetryCount = 5,
            SubscriberClientAppName = "EventBus.UnitTest",
            DefaultTopicName = "AppTopicName",
            EventBusType = EventBusType.RabbitMq,
            EventNameSuffix = "IntegrationEvent",
            //Connection = new ConnectionFactory()
            //{
            //    HostName = "localhost",
            //    Port = 5672,
            //    UserName = "guest",
            //    Password = "guest",
            //}
        };
    }
}