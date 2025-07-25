using System.Net.Sockets;
using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using EventBus.RabbitMq.Abstract;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMq;

public class EventBusRabbitMQ : BaseEventBus
{
    IRabbitMQPersistentConnection persistentConnection;
    private readonly IConnectionFactory connectionFactory;
    private readonly IChannel consumerChannel;

    public EventBusRabbitMQ(EventBusConfig config, IServiceProvider serviceProvider) : base(config, serviceProvider)
    {
        if (config.Connection != null)
        {
            var connJson = JsonConvert.SerializeObject(EventBusConfig.Connection, new JsonSerializerSettings()
            {
                // self referencing loop detected for property
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            });

            connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson);
        }

        connectionFactory = new ConnectionFactory();
        this.persistentConnection = new RabbitMQPersistentConnection(connectionFactory, EventBusConfig.ConnectionRetryCount);

        consumerChannel = CreateConsumerChannel();

        SubsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    /// <summary>
    /// Handles the event when a subscription is removed from the subscription manager.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventName"></param>
    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        eventName = ProcessEventName(eventName);

        if (!persistentConnection.IsConnected)
        {
            persistentConnection.TryConnect().GetAwaiter().GetResult();
        }

        // using var channel = persistentConnection.CreateModel().GetAwaiter().GetResult();

        consumerChannel.QueueUnbindAsync(queue: GetSubName(eventName), exchange: EventBusConfig.DefaultTopicName, routingKey: eventName).GetAwaiter().GetResult();

        if (SubsManager.IsEmpty)
        {
            consumerChannel.CloseAsync();
        }
    }

    /// <summary>
    /// Publishes an integration event to the RabbitMQ exchange.
    /// </summary>
    /// <param name="event"></param>
    public override void Publish(IntegrationEvent @event)
    {
        if (!persistentConnection.IsConnected)
        {
            persistentConnection.TryConnect();
        }

        var policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(EventBusConfig.ConnectionRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, time) =>
                {
                    // loglama yapılabilir
                });

        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);

        policy.ExecuteAsync(async () =>
        {
            var channel = await persistentConnection.CreateModel(); // IChannel

            await channel.ExchangeDeclareAsync(
                exchange: EventBusConfig.DefaultTopicName,
                type: "direct");

            await channel.QueueDeclareAsync(
                queue: GetSubName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent // persistent
            };

            await channel.BasicPublishAsync(
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);

            await channel.DisposeAsync(); // IChannel async dispose
        });
    }

    /// <summary>
    /// Subscribes to an event of type T with a handler of type TH.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            if (!persistentConnection.IsConnected)
            {
                persistentConnection.TryConnect().GetAwaiter().GetResult();
            }

            // Ensure queue exists while consuming
            consumerChannel.QueueDeclareAsync(queue: GetSubName(eventName), durable: true, exclusive: false, autoDelete: false, arguments: null).GetAwaiter().GetResult();

            consumerChannel.QueueBindAsync(queue: GetSubName(eventName), exchange: EventBusConfig.DefaultTopicName, routingKey: eventName).GetAwaiter().GetResult();
        }

        SubsManager.AddSubscription<T, TH>();

        StartBasicConsume(eventName);
    }

    /// <summary>
    /// Unsubscribes from an event of type T with a handler of type TH. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TH"></typeparam>
    public override void UnSubscribe<T, TH>()
    {
        SubsManager.RemoveSubscription<T, TH>();
    }

    /// <summary>
    /// Creates a consumer channel for RabbitMQ.
    /// </summary>
    /// <returns></returns>
    private IChannel CreateConsumerChannel()
    {
        if (!persistentConnection.IsConnected)
        {
            persistentConnection.TryConnect().GetAwaiter().GetResult();
        }

        var channel = persistentConnection.CreateModel().GetAwaiter().GetResult();

        channel.ExchangeDeclareAsync(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        return channel;
    }

    /// <summary>
    /// Starts consuming messages from the specified event queue.
    /// </summary>
    /// <param name="eventName"></param>
    private void StartBasicConsume(string eventName)
    {
        if (consumerChannel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(consumerChannel); // Async ?

            consumer.ReceivedAsync += Consumer_Received;

            consumerChannel.BasicConsumeAsync(queue: GetSubName(eventName), autoAck: false, consumer: consumer);
        }
    }

    /// <summary>
    /// Handles the received message from RabbitMQ and processes it asynchronously.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        eventName = ProcessEventName(eventName);
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            // logging
            throw;
        }

        await consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
    }
}
