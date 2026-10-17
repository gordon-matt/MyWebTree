using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyWebTree.Models;

namespace MyWebTree.Data;

/// <summary>Stores a <see cref="PartialDate"/> as <c>yyyy</c>, <c>yyyy-MM</c>, or <c>yyyy-MM-dd</c>.</summary>
public sealed class PartialDateConverter() : ValueConverter<PartialDate?, string?>(
    date => date == null ? null : date.Value.ToString(),
    text => PartialDate.FromStored(text))
{
    public static PartialDateConverter Instance { get; } = new();
}
