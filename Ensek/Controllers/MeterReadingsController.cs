using Ensek.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ensek.Controllers;

[ApiController]
[Route("meter-reading-uploads")]
public class MeterReadingsController(IMeterReadingService service) : ControllerBase
{
    private readonly IMeterReadingService _service = service;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Upload meter readings CSV", Description = "Processes a CSV and returns counts of successful and failed rows.")]
    [ProducesResponseType(typeof(MeterReadingUploadResult), StatusCodes.Status200OK)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<MeterReadingUploadResult>> Upload(IFormFile file, [FromQuery] bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        // Only allow .csv files by extension (case-insensitive)
        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .csv files are allowed.");

    var result = await _service.UploadAsync(file, includeDetails, cancellationToken);
        return Ok(result);
    }
}
