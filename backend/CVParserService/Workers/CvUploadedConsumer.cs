using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CVParserService.Services;
using Shared.Events;

namespace CVParserService.Workers;

public class CvUploadedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private const string QueueName = "cv.uploaded";

    private IConnection? _connection;
    private IChannel? _channel;

    public CvUploadedConsumer(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:User"] ?? "cvuser",
            Password = _configuration["RabbitMQ:Pass"] ?? "cvpass123"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var uploadedEvent = JsonSerializer.Deserialize<CvUploadedEvent>(json);

            if (uploadedEvent != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var downloadService = scope.ServiceProvider.GetRequiredService<MinioDownloadService>();
                var textExtractor = scope.ServiceProvider.GetRequiredService<PdfTextExtractor>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();

                Console.WriteLine($"CV alındı: {uploadedEvent.FileName}");

                using var pdfStream = await downloadService.DownloadFileAsync(uploadedEvent.FilePath);
                var rawText = textExtractor.ExtractText(pdfStream);

                var parsedEvent = new CvParsedEvent
                {
                    UserId = uploadedEvent.UserId,
                    JobDescription = uploadedEvent.JobDescription,
                    RawText = rawText
                };

                await publisher.PublishAsync(parsedEvent);

                Console.WriteLine($"CV parse edildi ve gönderildi: {parsedEvent.Id}");
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}