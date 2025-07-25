using System.Net.Sockets;
using EventBus.RabbitMq.Abstract;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMq;

public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection, IDisposable
{
    private IConnection connection;
    private readonly IConnectionFactory connectionFactory;
    private readonly int retryCount;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private bool disposed;

    public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
    {
        this.connectionFactory = connectionFactory;
        this.retryCount = retryCount;
    }

    public bool IsConnected => connection != null && connection.IsOpen;

    public async Task<IChannel> CreateModel()
    {
        return await connection.CreateChannelAsync();
    }

    public async Task<bool> TryConnect()
    {
        await semaphore.WaitAsync();
        try
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetryAsync(retryCount, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await policy.ExecuteAsync(async () =>
            {
                connection = await connectionFactory.CreateConnectionAsync();
            });

            if (IsConnected)
            {
                connection.ConnectionShutdownAsync += Connection_ConnectionShutdown;
                connection.CallbackExceptionAsync += Connection_CallbackException;
                connection.ConnectionBlockedAsync += Connection_ConnectionBlocked;

                return true;
            }

            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }
    private async Task Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        if (!disposed)
        {
            await TryConnect();
        }
    }

    private async Task Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (!disposed)
        {
            await TryConnect();
        }
    }

    private async Task Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (!disposed)
        {
            await TryConnect();
        }
    }

    public void Dispose()
    {
        disposed = true;
        connection.Dispose();
    }
}
