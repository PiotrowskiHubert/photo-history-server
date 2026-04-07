namespace photo_history_server.Application.Common.DTOs;

public record SystemRegisterRequest(string RegistrationKey, string Username, string Email, string Password);

