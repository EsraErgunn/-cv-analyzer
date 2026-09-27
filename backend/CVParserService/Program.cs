using Minio;
using CVParserService.Services;
using CVParserService.Workers;
using Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinio(config => config
    .WithEndpoint(builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["MinIO:User"] ?? "cvuser",
        builder.Configuration["MinIO:Pass"] ?? "cvpass123")
    .WithSSL(false));

builder.Services.AddScoped<MinioDownloadService>();
builder.Services.AddScoped<PdfTextExtractor>();
builder.Services.AddSingleton<CvSectionParser>();
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddHostedService<CvUploadedConsumer>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "CVParserService" }));

app.Run();
