using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using photo_history_server.Application.Users;

namespace photo_history_server.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,System")]
public class AdminController : ControllerBase
{
    private readonly AdminUserService _adminUserService;

    public AdminController(AdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>
    /// Get all registered users. Accessible by Admin and System roles only.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _adminUserService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Get all photos from all users, sorted by upload date descending.
    /// Accessible by Admin and System roles only.
    /// </summary>
    [HttpGet("photos")]
    public async Task<IActionResult> GetPhotos()
    {
        var photos = await _adminUserService.GetAllPhotosAsync();
        return Ok(photos);
    }

    /// <summary>
    /// Returns the count of photos awaiting review.
    /// </summary>
    [HttpGet("photos/unreviewed-count")]
    public async Task<IActionResult> GetUnreviewedCount()
    {
        var count = await _adminUserService.GetUnreviewedCountAsync();
        return Ok(new { count });
    }

    /// <summary>
    /// Marks a photo as reviewed (approved) by the current admin.
    /// </summary>
    [HttpPost("photos/{id:guid}/review")]
    public async Task<IActionResult> ReviewPhoto(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var adminId))
            return Unauthorized(new { Message = "Invalid token." });

        var ok = await _adminUserService.ReviewPhotoAsync(id, adminId);
        return ok ? NoContent() : NotFound(new { Message = "Photo not found." });
    }

    /// <summary>
    /// Reverts a photo to unreviewed status.
    /// </summary>
    [HttpDelete("photos/{id:guid}/review")]
    public async Task<IActionResult> UnreviewPhoto(Guid id)
    {
        var ok = await _adminUserService.UnreviewPhotoAsync(id);
        return ok ? NoContent() : NotFound(new { Message = "Photo not found." });
    }
}
