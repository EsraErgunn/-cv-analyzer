using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Shared.Messaging;

// Singleton olarak kaydedilir: bağlantı ilk yayında açılır ve servis ömrü boyunca paylaşılır.
public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _declaredQueues = new();

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _factory = RabbitMqConnectionFactory.Create(configuration);
    }

    public async Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        // IChannel thread-safe değildir; yayınlar tek kanaldan sırayla yapılır.
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            if (_declaredQueues.Add(queue))
                await RabbitMqConnectionFactory.DeclareQueueAsync(channel, queue, cancellationToken);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        _connection ??= await _factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _declaredQueues.Clear();
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        _lock.Dispose();
    }
}
