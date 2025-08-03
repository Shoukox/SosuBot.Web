using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SosuBot.Web.Constants;
using SosuBot.Web.Services;

namespace SosuBot.Web.Controllers;

[ApiController]
public class FileController (
    ILogger<FileController> logger,
    RabbitMqService rabbitMqService) : ControllerBase
{
    [RequestSizeLimit(1073741824)]
    [HttpPost]
    [Route("/upload-video")]
    public async Task<IActionResult> PostVideo(IFormFile file, string message)
    {
        if (file.Length == 0)
            return BadRequest("No file provided or file is empty");

        if (!file.ContentType.StartsWith("video/"))
            return BadRequest("Invalid file type. Only video files are allowed.");

        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrEmpty(fileName))
            return BadRequest("Invalid filename");

        var savePath = Path.Combine(FilePathConstants.VideoPath, message[..^4] + ".mp4");

        try
        {
            await using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        
            await rabbitMqService.AckMessageAsync(message);
            logger.LogInformation($"Successfully rendered video: {message}");
            return Ok(new { fileName = message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving video file for Job {JobId}", message);
            return StatusCode(500, "Error saving file");
        }
    }
    
    [HttpGet]
    [Route("/get-replay")]
    public async Task<IActionResult> GetReplay(string replayName)
    {
        string path = Path.Combine(FilePathConstants.ReplaysPath, replayName);
        if (!System.IO.File.Exists(path))
            return NotFound("File not found");

        return File(System.IO.File.OpenRead(path), "text/plain", enableRangeProcessing: true);
    }
}