using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyWebTree.Data;

/// <summary>
/// Timestamps are written as UTC, but SQLite and the other providers hand them back with
/// <see cref="DateTimeKind.Unspecified"/>. Re-stamping on read keeps <c>ToLocalTime()</c> honest.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    public static UtcDateTimeConverter Instance { get; } = new();
}
