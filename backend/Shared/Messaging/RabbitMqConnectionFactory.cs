using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Shared.Messaging;

public static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(IConfiguration configuration) => new()
    {
        HostName = configuration["RabbitMQ:Host"] ?? "localhost",
        Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
        UserName = configuration["RabbitMQ:User"] ?? "cvuser",
        Password = configuration["RabbitMQ:Pass"] ?? "cvpass123",
        AutomaticRecoveryEnabled = true
    };

    public static Task DeclareQueueAsync(IChannel channel, string queue, CancellationToken cancellationToken = default) =>
        channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
}
