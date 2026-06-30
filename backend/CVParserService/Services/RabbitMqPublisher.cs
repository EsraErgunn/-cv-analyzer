using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Events;

namespace CVParserService.Services;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string QueueName = "cv.parsed";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "cvuser",
            Password = configuration["RabbitMQ:Pass"] ?? "cvpass123"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(CvParsedEvent parsedEvent)
    {
        var json = JsonSerializer.Serialize(parsedEvent);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            body: body);
    }

    public void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
    }
}