namespace ClawdFiles.Application.Interfaces;

public interface IShortCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
