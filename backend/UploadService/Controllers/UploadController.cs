using Microsoft.AspNetCore.Mvc;
using Shared.Events;
using UploadService.Services;

namespace UploadService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly MinioStorageService _storageService;
    private readonly RabbitMqPublisher _publisher;

    public UploadController(MinioStorageService storageService, RabbitMqPublisher publisher)
    {
        _storageService = storageService;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> UploadCv(
        IFormFile file,
        [FromForm] string jobDescription,
        [FromForm] Guid userId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("CV dosyası gerekli.");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Sadece PDF dosyası kabul edilir.");

        using var stream = file.OpenReadStream();
        var filePath = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        var cvEvent = new CvUploadedEvent
        {
            UserId = userId,
            FileName = file.FileName,
            FilePath = filePath,
            JobDescription = jobDescription
        };

        await _publisher.PublishAsync(cvEvent);

        return Ok(new { message = "CV yüklendi, analiz başladı.", eventId = cvEvent.Id });
    }
}