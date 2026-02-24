using System.Security.Cryptography;
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Infrastructure.Services;

public class RandomShortCodeGenerator(IFileHeaderRepository fileRepo) : IShortCodeGenerator
{
    private static readonly char[] Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = GenerateCode(6);
            if (await fileRepo.FindByShortCodeAsync(code, ct) is null) return code;
        }
        throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts");
    }

    private static string GenerateCode(int length)
    {
        return string.Create(length, (object?)null, (span, _) =>
        {
            Span<byte> random = stackalloc byte[length];
            RandomNumberGenerator.Fill(random);
            for (var i = 0; i < length; i++)
                span[i] = Chars[random[i] % Chars.Length];
        });
    }
}
