using AIAnalyzerService.Analyzers;
using Shared.Events;
using Shared.Messaging;

namespace AIAnalyzerService.Workers;

public class CvParsedConsumer : RabbitMqConsumer<CvParsedEvent>
{
    public CvParsedConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<CvParsedConsumer> logger)
        : base(serviceProvider, configuration, logger)
    {
    }

    protected override string QueueName => QueueNames.CvParsed;

    protected override async Task HandleAsync(CvParsedEvent parsedEvent, IServiceProvider services, CancellationToken cancellationToken)
    {
        var analyzer = services.GetRequiredService<ICvAnalyzer>();
        var publisher = services.GetRequiredService<RabbitMqPublisher>();

        Logger.LogInformation("CV analiz ediliyor: {CvId}", parsedEvent.CvId);

        var analysis = await analyzer.AnalyzeAsync(parsedEvent, cancellationToken);

        var analyzedEvent = new CvAnalyzedEvent
        {
            CvId = parsedEvent.CvId,
            UserId = parsedEvent.UserId,
            FileName = parsedEvent.FileName,
            AtsScore = analysis.AtsScore,
            MatchedSkills = analysis.MatchedSkills,
            MissingSkills = analysis.MissingSkills,
            Suggestions = analysis.Suggestions,
            Summary = analysis.Summary,
            ImprovedCvText = analysis.ImprovedCvText,
            AnalyzerName = analyzer.Name
        };

        await publisher.PublishAsync(QueueNames.CvAnalyzed, analyzedEvent, cancellationToken);

        Logger.LogInformation("CV analiz edildi: {CvId}, ATS puanı {Score} ({Analyzer})",
            analyzedEvent.CvId, analyzedEvent.AtsScore, analyzer.Name);
    }
}
