using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Application.Users;
namespace photo_history_server.Api.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly SystemRegistrationService _systemRegistrationService;
    private readonly AdminRegistrationService _adminRegistrationService;

    public AuthController(
        AuthService authService,
        SystemRegistrationService systemRegistrationService,
        AdminRegistrationService adminRegistrationService)
    {
        _authService = authService;
        _systemRegistrationService = systemRegistrationService;
        _adminRegistrationService = adminRegistrationService;
    }
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Status switch
        {
            RegisterStatus.Success       => Ok(result.Response),
            RegisterStatus.EmailTaken    => Conflict(new { Message = "Email is already in use.", Field = "email" }),
            RegisterStatus.UsernameTaken => Conflict(new { Message = "Username is already taken.", Field = "username" }),
            _                            => StatusCode(500)
        };
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Status switch
        {
            LoginStatus.Success            => Ok(result.Response),
            LoginStatus.Banned             => StatusCode(403, new { Message = "Your account has been banned due to community guidelines violation.", Code = "BANNED" }),
            LoginStatus.Inactive           => StatusCode(403, new { Message = "Your account has been deactivated. Please contact support.", Code = "INACTIVE" }),
            LoginStatus.InvalidCredentials => Unauthorized(new { Message = "Invalid email/username or password." }),
            _                              => StatusCode(500)
        };
    }

    [AllowAnonymous]
    [HttpPost("register-system")]
    public async Task<IActionResult> RegisterSystem([FromBody] SystemRegisterRequest request)
    {
        var registerRequest = new RegisterRequest(request.Username, request.Email, request.Password);
        var (response, result) = await _systemRegistrationService.RegisterSystemAsync(
            request.RegistrationKey, registerRequest);

        return result switch
        {
            SystemRegistrationResult.Success => Ok(response),
            SystemRegistrationResult.AlreadyExists => Conflict(new { Message = "System user already exists." }),
            SystemRegistrationResult.InvalidKey => Forbid(),
            _ => StatusCode(500)
        };
    }

    /// <summary>
    /// Records logout time for the authenticated user. Requires authentication.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Message = "Invalid token." });

        await _authService.LogoutAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Register a new Admin user. Protected by a hardcoded registration key.
    /// Multiple admins are allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegisterRequest request)
    {
        var (response, result) = await _adminRegistrationService.RegisterAdminAsync(
            request.RegistrationKey,
            new RegisterRequest(request.Username, request.Email, request.Password));

        return result switch
        {
            AdminRegistrationResult.Success => Ok(response),
            AdminRegistrationResult.InvalidKey => Forbid(),
            AdminRegistrationResult.EmailTaken => Conflict(new { Message = "Email is already taken." }),
            _ => StatusCode(500)
        };
    }
}
