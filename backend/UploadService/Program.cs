using Minio;
using UploadService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMinio(config => config
    .WithEndpoint(builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["MinIO:User"] ?? "cvuser",
        builder.Configuration["MinIO:Pass"] ?? "cvpass123")
    .WithSSL(false));

builder.Services.AddScoped<MinioStorageService>();
builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

app.Run();