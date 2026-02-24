namespace ClawdFiles.Application.DTOs;

public record ErrorResponse(string Error, string? Hint = null);
