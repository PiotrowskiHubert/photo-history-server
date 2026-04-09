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
}
