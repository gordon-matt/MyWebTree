using System.Globalization;

namespace MyWebTree.Models;

/// <summary>
/// A date that may be only a year, a year and month, or a full day.
/// Family records are often incomplete, so a host can round-trip "1842" without inventing a month.
/// </summary>
public readonly record struct PartialDate : IComparable<PartialDate>
{
    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public PartialDate(int year, int? month = null, int? day = null)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (day is not null && month is null)
        {
            throw new ArgumentOutOfRangeException(nameof(day), "A day needs a month.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        if (day is int value && (value < 1 || value > DateTime.DaysInMonth(year, month!.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(day));
        }

        Year = year;
        Month = month;
        Day = day;
    }

    public static bool TryCreate(int year, int? month, int? day, out PartialDate date)
    {
        if (year is < 1 or > 9999 || (day is not null && month is null) || month is < 1 or > 12)
        {
            date = default;
            return false;
        }

        if (day is int value && (value < 1 || value > DateTime.DaysInMonth(year, month!.Value)))
        {
            date = default;
            return false;
        }

        date = new PartialDate(year, month, day);
        return true;
    }

    /// <summary>Reads <c>yyyy</c>, <c>yyyy-MM</c>, or <c>yyyy-MM-dd</c>. Anything else is ignored.</summary>
    public static PartialDate? FromStored(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string[] parts = text.Trim().Split('-');
        if (parts.Length is < 1 or > 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int year))
        {
            return null;
        }

        int? month = null;
        int? day = null;
        if (parts.Length > 1)
        {
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int parsedMonth))
            {
                return null;
            }

            month = parsedMonth;
        }

        if (parts.Length > 2)
        {
            if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int parsedDay))
            {
                return null;
            }

            day = parsedDay;
        }

        return TryCreate(year, month, day, out var date) ? date : null;
    }

    /// <summary>
    /// True when every part both dates actually know says this one is earlier.
    /// A year-only death in the same year as a full birth is not treated as earlier.
    /// </summary>
    public bool IsBefore(PartialDate other)
    {
        if (Year != other.Year)
        {
            return Year < other.Year;
        }

        if (Month is null || other.Month is null || Month == other.Month && (Day is null || other.Day is null))
        {
            return false;
        }

        if (Month != other.Month)
        {
            return Month < other.Month;
        }

        return Day < other.Day;
    }

    public int CompareTo(PartialDate other)
    {
        int year = Year.CompareTo(other.Year);
        if (year != 0)
        {
            return year;
        }

        int month = (Month ?? 0).CompareTo(other.Month ?? 0);
        return month != 0 ? month : (Day ?? 0).CompareTo(other.Day ?? 0);
    }

    public override string ToString()
    {
        if (Month is int month && Day is int day)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Year:0000}-{month:00}-{day:00}");
        }

        if (Month is int monthOnly)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Year:0000}-{monthOnly:00}");
        }

        return Year.ToString("0000", CultureInfo.InvariantCulture);
    }
}
