using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ResultService.Data;

namespace ResultService.Services;

/// <summary>
/// Cache-aside: okumada önce Redis'e bakılır, yoksa PostgreSQL'den okunup cache'e yazılır.
/// Yazmada PostgreSQL güncellenir ve cache tazelenir. Redis erişilemezse doğrudan veritabanı kullanılır.
/// </summary>
public class ResultStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    private readonly ResultDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ResultStore> _logger;

    public ResultStore(ResultDbContext db, IDistributedCache cache, ILogger<ResultStore> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CvAnalysisResult?> GetAsync(Guid cvId, CancellationToken cancellationToken)
    {
        var cached = await TryCacheAsync(() => _cache.GetStringAsync(CacheKey(cvId), cancellationToken));
        if (cached != null)
            return JsonSerializer.Deserialize<CvAnalysisResult>(cached);

        var result = await _db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.CvId == cvId, cancellationToken);
        if (result != null)
            await CacheAsync(result, cancellationToken);

        return result;
    }

    public Task<List<CvAnalysisResult>> GetByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.Results.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(CvAnalysisResult result, CancellationToken cancellationToken)
    {
        // Aynı event tekrar gelirse (at-least-once teslimat) kayıt çoğaltılmaz, güncellenir.
        var existing = await _db.Results.FirstOrDefaultAsync(r => r.CvId == result.CvId, cancellationToken);
        if (existing == null)
            _db.Results.Add(result);
        else
            _db.Entry(existing).CurrentValues.SetValues(result);

        await _db.SaveChangesAsync(cancellationToken);
        await CacheAsync(result, cancellationToken);
    }

    private Task CacheAsync(CvAnalysisResult result, CancellationToken cancellationToken) =>
        TryCacheAsync(async () =>
        {
            await _cache.SetStringAsync(CacheKey(result.CvId), JsonSerializer.Serialize(result), CacheOptions, cancellationToken);
            return string.Empty;
        });

    private async Task<string?> TryCacheAsync(Func<Task<string?>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Redis erişilemedi, cache atlanıyor: {Message}", ex.Message);
            return null;
        }
    }

    private static string CacheKey(Guid cvId) => $"cv-result:{cvId}";
}
