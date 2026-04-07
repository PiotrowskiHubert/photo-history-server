namespace photo_history_server.Application.Common.DTOs;
public record AuthResponse(string Token, string Username, string Email, string Role, string? AvatarUrl);
