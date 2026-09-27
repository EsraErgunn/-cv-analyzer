using Microsoft.EntityFrameworkCore;
using Npgsql;
using ResultService.Data;
using ResultService.Services;
using ResultService.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// "ConnectionStrings:ResultDb" verilirse o kullanılır; yoksa Postgres:* ayarlarından oluşturulur.
var connectionString = builder.Configuration.GetConnectionString("ResultDb")
    ?? new NpgsqlConnectionStringBuilder
    {
        Host = builder.Configuration["Postgres:Host"] ?? "localhost",
        Port = int.TryParse(builder.Configuration["Postgres:Port"], out var pgPort) ? pgPort : 5432,
        Database = builder.Configuration["Postgres:Db"] ?? "cvanalyzer",
        Username = builder.Configuration["Postgres:User"] ?? "cvuser",
        Password = builder.Configuration["Postgres:Pass"] ?? "cvpass123"
    }.ConnectionString;

builder.Services.AddDbContext<ResultDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
    options.InstanceName = "cv-analyzer:";
});

builder.Services.AddScoped<ResultStore>();
builder.Services.AddHostedService<CvAnalyzedConsumer>();

var app = builder.Build();

await EnsureDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI();

var results = app.MapGroup("/api/results").WithTags("Results");

results.MapGet("/{cvId:guid}", async (Guid cvId, ResultStore store, CancellationToken ct) =>
{
    var result = await store.GetAsync(cvId, ct);
    // Analiz asenkron olduğundan sonuç henüz yoksa istemci bir süre sonra tekrar sorgular.
    return result is null
        ? Results.NotFound(new { cvId, status = "processing", message = "Analiz henüz tamamlanmadı veya CV bulunamadı." })
        : Results.Ok(result);
});

results.MapGet("/user/{userId:guid}", async (Guid userId, ResultStore store, CancellationToken ct) =>
    Results.Ok(await store.GetByUserAsync(userId, ct)));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ResultService" }));

app.Run();

static async Task EnsureDatabaseAsync(WebApplication app)
{
    // Tablo yoksa oluşturulur. Şema büyüdükçe EF Core migration'larına geçilmesi önerilir.
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ResultDbContext>().Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception ex) when (attempt < 10)
        {
            app.Logger.LogWarning("PostgreSQL hazır değil ({Message}), tekrar denenecek ({Attempt}/10).", ex.Message, attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
