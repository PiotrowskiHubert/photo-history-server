namespace photo_history_server.Application.Common.DTOs;
public record AdminRegisterRequest(
    string RegistrationKey,
    string Username,
    string Email,
    string Password);
