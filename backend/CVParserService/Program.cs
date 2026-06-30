using Minio;
using CVParserService.Services;
using CVParserService.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinio(config => config
    .WithEndpoint(builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["MinIO:User"] ?? "cvuser",
        builder.Configuration["MinIO:Pass"] ?? "cvpass123")
    .WithSSL(false));

builder.Services.AddScoped<MinioDownloadService>();
builder.Services.AddScoped<PdfTextExtractor>();
builder.Services.AddScoped<RabbitMqPublisher>();

builder.Services.AddHostedService<CvUploadedConsumer>();

var app = builder.Build();

app.Run();