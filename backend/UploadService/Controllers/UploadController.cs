using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using Shared.Messaging;
using UploadService.Services;

namespace UploadService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    private readonly MinioStorageService _storageService;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<UploadController> _logger;

    public UploadController(MinioStorageService storageService, RabbitMqPublisher publisher, ILogger<UploadController> logger)
    {
        _storageService = storageService;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCv(
        IFormFile file,
        [FromForm] string jobDescription,
        [FromForm] Guid userId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("CV dosyası gerekli.");

        if (file.Length > MaxFileSize)
            return BadRequest("CV dosyası en fazla 10 MB olabilir.");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Sadece PDF dosyası kabul edilir.");

        if (string.IsNullOrWhiteSpace(jobDescription))
            return BadRequest("İş ilanı metni (jobDescription) gerekli.");

        await using var stream = file.OpenReadStream();

        // Content-Type istemciden gelir; dosyanın gerçekten PDF olduğunu imzasından doğrula.
        if (!await HasPdfSignatureAsync(stream, cancellationToken))
            return BadRequest("Dosya geçerli bir PDF değil.");

        var filePath = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        var cvEvent = new CvUploadedEvent
        {
            UserId = userId,
            FileName = file.FileName,
            FilePath = filePath,
            JobDescription = jobDescription
        };

        await _publisher.PublishAsync(QueueNames.CvUploaded, cvEvent, cancellationToken);

        _logger.LogInformation("CV yüklendi: {CvId} ({FileName})", cvEvent.Id, cvEvent.FileName);

        return Accepted(new
        {
            message = "CV yüklendi, analiz başladı.",
            cvId = cvEvent.Id,
            resultUrl = $"/api/results/{cvEvent.Id}"
        });
    }

    private static async Task<bool> HasPdfSignatureAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[PdfSignature.Length];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        stream.Position = 0;
        return read == header.Length && header.AsSpan().SequenceEqual(PdfSignature);
    }
}
