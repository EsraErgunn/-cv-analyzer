using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging;

/// <summary>
/// Bir kuyruğu dinleyen temel arka plan servisi. Her mesaj kendi DI scope'unda işlenir,
/// başarılıysa ack'lenir; hata olursa mesaj "&lt;kuyruk&gt;.error" kuyruğuna taşınır.
/// </summary>
public abstract class RabbitMqConsumer<TMessage> : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    private IConnection? _connection;
    private IChannel? _channel;

    protected RabbitMqConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        Logger = logger;
    }

    protected abstract string QueueName { get; }

    protected ILogger Logger { get; }

    protected abstract Task HandleAsync(TMessage message, IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(_configuration);

        // RabbitMQ (ör. docker-compose ile) servisten sonra ayağa kalkabilir; hazır olana kadar tekrar dene.
        while (_connection == null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogWarning("RabbitMQ'ya bağlanılamadı ({Message}), {Delay} sn sonra tekrar denenecek.",
                    ex.Message, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }

        if (_connection == null)
            return;

        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await RabbitMqConnectionFactory.DeclareQueueAsync(_channel, QueueName, stoppingToken);
        await RabbitMqConnectionFactory.DeclareQueueAsync(_channel, QueueNames.ErrorQueueFor(QueueName), stoppingToken);

        // Her instance aynı anda tek mesaj işler; yatay ölçekleme için servis replikası eklenir.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) => OnMessageAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        Logger.LogInformation("'{Queue}' kuyruğu dinleniyor.", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var channel = _channel!;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var message = JsonSerializer.Deserialize<TMessage>(json)
                ?? throw new InvalidOperationException("Mesaj boş veya çözümlenemedi.");

            using var scope = _serviceProvider.CreateScope();
            await HandleAsync(message, scope.ServiceProvider, stoppingToken);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Kapanış sırasında yarım kalan mesaj kuyruğa geri dönsün.
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "'{Queue}' mesajı işlenemedi, hata kuyruğuna taşınıyor.", QueueName);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueNames.ErrorQueueFor(QueueName),
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true, ContentType = "application/json" },
                body: ea.Body);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
    }
}
