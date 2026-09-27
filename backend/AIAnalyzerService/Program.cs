using AIAnalyzerService.Analyzers;
using AIAnalyzerService.Workers;
using Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<ICvAnalyzer, RuleBasedCvAnalyzer>();

builder.Services.AddHostedService<CvParsedConsumer>();

var app = builder.Build();

app.MapGet("/health", (ICvAnalyzer analyzer) => Results.Ok(new
{
    status = "healthy",
    service = "AIAnalyzerService",
    analyzer = analyzer.Name
}));

app.Run();
