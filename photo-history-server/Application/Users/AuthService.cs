using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using photo_history_server.Application.Common.DTOs;
using photo_history_server.Domain.Entities;
using photo_history_server.Domain.Enums;
using photo_history_server.Infrastructure.Persistence;
namespace photo_history_server.Application.Users;
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    /// <summary>
    /// Register a new user with email and password.
    /// Returns null if email is already taken.
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        bool emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists) return null;
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        var token = GenerateJwt(user);
        return new AuthResponse(token, user.Username, user.Email, user.Role.ToString(), user.AvatarUrl);
    }
    /// <summary>
    /// Authenticate user with email and password.
    /// Returns null if credentials are invalid.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || user.PasswordHash is null) return null;
        bool valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!valid) return null;
        var token = GenerateJwt(user);
        return new AuthResponse(token, user.Username, user.Email, user.Role.ToString(), user.AvatarUrl);
    }
    /// <summary>
    /// Generate a JWT token for the given user. Public so other services can reuse it.
    /// </summary>
    public string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
