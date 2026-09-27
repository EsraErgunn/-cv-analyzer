using CVParserService.Services;
using Shared.Events;
using Shared.Messaging;

namespace CVParserService.Workers;

public class CvUploadedConsumer : RabbitMqConsumer<CvUploadedEvent>
{
    public CvUploadedConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<CvUploadedConsumer> logger)
        : base(serviceProvider, configuration, logger)
    {
    }

    protected override string QueueName => QueueNames.CvUploaded;

    protected override async Task HandleAsync(CvUploadedEvent uploadedEvent, IServiceProvider services, CancellationToken cancellationToken)
    {
        var downloadService = services.GetRequiredService<MinioDownloadService>();
        var textExtractor = services.GetRequiredService<PdfTextExtractor>();
        var sectionParser = services.GetRequiredService<CvSectionParser>();
        var publisher = services.GetRequiredService<RabbitMqPublisher>();

        Logger.LogInformation("CV alındı: {CvId} ({FileName})", uploadedEvent.Id, uploadedEvent.FileName);

        using var pdfStream = await downloadService.DownloadFileAsync(uploadedEvent.FilePath);
        var rawText = textExtractor.ExtractText(pdfStream);

        if (string.IsNullOrWhiteSpace(rawText))
            throw new InvalidOperationException(
                $"CV'den metin çıkarılamadı (taranmış/görsel PDF olabilir): {uploadedEvent.FileName}");

        var parsed = sectionParser.Parse(rawText);

        var parsedEvent = new CvParsedEvent
        {
            CvId = uploadedEvent.Id,
            UserId = uploadedEvent.UserId,
            FileName = uploadedEvent.FileName,
            JobDescription = uploadedEvent.JobDescription,
            Skills = parsed.Skills,
            Experience = parsed.Experience,
            Education = parsed.Education,
            RawText = rawText
        };

        await publisher.PublishAsync(QueueNames.CvParsed, parsedEvent, cancellationToken);

        Logger.LogInformation("CV parse edildi: {CvId}, {SkillCount} beceri bulundu.", parsedEvent.CvId, parsed.Skills.Count);
    }
}
