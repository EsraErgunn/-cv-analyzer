using ResultService.Data;
using ResultService.Services;
using Shared.Events;
using Shared.Messaging;

namespace ResultService.Workers;

public class CvAnalyzedConsumer : RabbitMqConsumer<CvAnalyzedEvent>
{
    public CvAnalyzedConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<CvAnalyzedConsumer> logger)
        : base(serviceProvider, configuration, logger)
    {
    }

    protected override string QueueName => QueueNames.CvAnalyzed;

    protected override async Task HandleAsync(CvAnalyzedEvent analyzedEvent, IServiceProvider services, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<ResultStore>();

        await store.SaveAsync(new CvAnalysisResult
        {
            CvId = analyzedEvent.CvId,
            UserId = analyzedEvent.UserId,
            FileName = analyzedEvent.FileName,
            AtsScore = analyzedEvent.AtsScore,
            MatchedSkills = analyzedEvent.MatchedSkills,
            MissingSkills = analyzedEvent.MissingSkills,
            Suggestions = analyzedEvent.Suggestions,
            Summary = analyzedEvent.Summary,
            ImprovedCvText = analyzedEvent.ImprovedCvText,
            AnalyzerName = analyzedEvent.AnalyzerName,
            AnalyzedAt = analyzedEvent.AnalyzedAt
        }, cancellationToken);

        Logger.LogInformation("Analiz sonucu kaydedildi: {CvId}", analyzedEvent.CvId);
    }
}
