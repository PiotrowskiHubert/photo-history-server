using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Application.Photos;

namespace photo_history_server.Api.Controllers;

[ApiController]
[Route("api/photos")]
public class PhotosController : ControllerBase
{
    private readonly PhotoService _photoService;

    public PhotosController(PhotoService photoService)
    {
        _photoService = photoService;
    }

    /// <summary>
    /// Upload a photo with metadata. Requires authentication.
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadPhotoRequest request)
    {
        // Validate file presence
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { Message = "File is required." });

        // Validate content type
        if (!request.File.ContentType.StartsWith("image/"))
            return BadRequest(new { Message = "Only image files are allowed." });

        // Validate file size (max 10 MB)
        const long maxSize = 10 * 1024 * 1024;
        if (request.File.Length > maxSize)
            return BadRequest(new { Message = "File size must not exceed 10 MB." });

        // Parse latitude and longitude using InvariantCulture to avoid decimal separator issues
        if (!double.TryParse(request.Latitude, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var latitude))
            return BadRequest(new { Message = "Invalid latitude value." });

        if (!double.TryParse(request.Longitude, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var longitude))
            return BadRequest(new { Message = "Invalid longitude value." });

        // Parse TakenAt from ISO 8601 string
        DateTime? takenAt = null;
        if (!string.IsNullOrWhiteSpace(request.TakenAt))
        {
            if (DateTime.TryParse(request.TakenAt, null,
                DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                takenAt = parsedDate;
            }
            else
            {
                return BadRequest(new { Message = "Invalid takenAt date format. Use ISO 8601." });
            }
        }

        // Extract user id from JWT claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Message = "Invalid token." });

        var parsed = new ParsedUploadPhotoRequest(
            request.File, request.Description, takenAt,
            latitude, longitude, request.Address);

        var result = await _photoService.UploadAsync(parsed, userId);
        return Created($"/uploads/{result.FileName}", result);
    }

    /// <summary>
    /// Get photos within a bounding box as lightweight map markers. Public endpoint.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string minLat,
        [FromQuery] string maxLat,
        [FromQuery] string minLng,
        [FromQuery] string maxLng)
    {
        // Parse all four bounds using InvariantCulture
        if (!double.TryParse(minLat, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMinLat) ||
            !double.TryParse(maxLat, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMaxLat) ||
            !double.TryParse(minLng, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMinLng) ||
            !double.TryParse(maxLng, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedMaxLng))
        {
            return BadRequest(new { Message = "Invalid bounding box parameters. Use invariant decimal format." });
        }

        var photos = await _photoService.GetInBoundsAsync(
            parsedMinLat, parsedMaxLat, parsedMinLng, parsedMaxLng);
        return Ok(photos);
    }

    /// <summary>
    /// Get full photo details by ID. Public endpoint.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var photo = await _photoService.GetByIdAsync(id);
        if (photo is null)
            return NotFound(new { Message = "Photo not found." });
        return Ok(photo);
    }
}

