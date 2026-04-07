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

    public AuthController(AuthService authService, SystemRegistrationService systemRegistrationService)
    {
        _authService = authService;
        _systemRegistrationService = systemRegistrationService;
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result is null)
            return Conflict(new { Message = "Email is already taken." });
        return Ok(result);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { Message = "Invalid email or password." });
        return Ok(result);
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
}
