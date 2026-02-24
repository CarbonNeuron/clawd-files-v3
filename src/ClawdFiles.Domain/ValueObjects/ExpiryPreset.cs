namespace ClawdFiles.Domain.ValueObjects;

public static class ExpiryPreset
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    private static readonly Dictionary<string, TimeSpan?> KnownPresets = new()
    {
        ["1h"] = TimeSpan.FromHours(1),
        ["6h"] = TimeSpan.FromHours(6),
        ["12h"] = TimeSpan.FromHours(12),
        ["1d"] = TimeSpan.FromDays(1),
        ["3d"] = TimeSpan.FromDays(3),
        ["1w"] = TimeSpan.FromDays(7),
        ["2w"] = TimeSpan.FromDays(14),
        ["1m"] = TimeSpan.FromDays(30),
        ["never"] = null,
    };

    public static TimeSpan? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DefaultExpiry;

        if (KnownPresets.TryGetValue(value.ToLowerInvariant(), out var preset))
            return preset;

        if (long.TryParse(value, out var seconds))
            return TimeSpan.FromSeconds(seconds);

        throw new ArgumentException($"Invalid expiry preset: '{value}'");
    }
}
