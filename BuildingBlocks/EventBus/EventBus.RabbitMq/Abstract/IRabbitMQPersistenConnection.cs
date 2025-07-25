using RabbitMQ.Client;

namespace EventBus.RabbitMq.Abstract;

public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    Task<bool> TryConnect();
    Task<IChannel> CreateModel();
}
