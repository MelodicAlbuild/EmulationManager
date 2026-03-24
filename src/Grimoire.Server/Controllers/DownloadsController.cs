using Grimoire.Server.Services;
using Grimoire.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Grimoire.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("downloads")]
public class DownloadsController : ControllerBase
{
    private readonly IFileProxyService _fileProxy;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(IFileProxyService fileProxy, ILogger<DownloadsController> logger)
    {
        _fileProxy = fileProxy;
        _logger = logger;
    }

    [HttpGet("{type}/{id:int}")]
    public async Task<IActionResult> Download(DownloadableType type, int id)
    {
        var fileInfo = await _fileProxy.ResolveFileAsync(type, id);
        if (fileInfo is null)
            return NotFound();

        if (!System.IO.File.Exists(fileInfo.PhysicalPath))
        {
            _logger.LogWarning("File not found on storage: {FileName} at {Path}", fileInfo.FileName, fileInfo.PhysicalPath);
            return NotFound(new { error = "File not found on storage", path = fileInfo.FileName });
        }

        _logger.LogInformation("Serving download: {Type}/{Id} ({FileName}, {Size} bytes)",
            type, id, fileInfo.FileName, fileInfo.FileSize);

        return PhysicalFile(
            fileInfo.PhysicalPath,
            fileInfo.ContentType,
            fileInfo.FileName,
            enableRangeProcessing: true
        );
    }

    [HttpHead("{type}/{id:int}")]
    public async Task<IActionResult> GetFileInfo(DownloadableType type, int id)
    {
        var fileInfo = await _fileProxy.ResolveFileAsync(type, id);
        if (fileInfo is null)
            return NotFound();

        Response.Headers.ContentLength = fileInfo.FileSize;
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.ContentType = fileInfo.ContentType;
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileInfo.FileName}\"";
        return Ok();
    }
}
