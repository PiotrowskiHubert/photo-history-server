namespace photo_history_server.Application.Common.DTOs;

public record AdminUserResponse(
    Guid Id,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? LastLogoutAt,
    bool IsActive,
    bool IsBanned);

