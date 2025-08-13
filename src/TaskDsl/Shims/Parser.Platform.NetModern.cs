using System.Globalization;
using System.Text.RegularExpressions;

#if NET6_0_OR_GREATER
namespace TaskDsl;

public static partial class Parser
{
    private static partial DateOnly NextOrSameDate(DateTimeOffset localNow, DayOfWeek target, bool inclusive)
    {
        var start = DateOnly.FromDateTime(localNow.Date);
        var delta = ((int)target - (int)start.DayOfWeek + 7) % 7;
        if (delta == 0 && !inclusive) delta = 7;
        return start.AddDays(delta);
    }

    private static partial TimeOnly ParseTimeToken(string s)
    {
        s = s.Trim().ToLowerInvariant();

        if (Regex.IsMatch(s, @"^\d{1,2}$"))
        {
            var m = int.Parse(s);
            if (m is < 0 or > 59) throw new FormatException("Minute token must be 0..59.");
            return new TimeOnly(0, m);
        }

        if (s.EndsWith("a") || s.EndsWith("p"))
        {
            var pm = s.EndsWith("p");
            s = s[..^1];
            var bits = s.Split(':');
            var h = int.Parse(bits[0]);
            var m = bits.Length > 1 ? int.Parse(bits[1]) : 0;
            if (h == 12) h = 0;
            if (pm) h += 12;
            return new TimeOnly(h, m);
        }

        return TimeOnly.Parse(s, CultureInfo.InvariantCulture);
    }

    private static partial string HexLower6(byte[] bytes)
    {
        // Convert.ToHexString exists on net5+; slice to 6 and lower for consistency
        return Convert.ToHexString(bytes).ToLowerInvariant()[..6];
    }

    private static partial TimeOnly DefaultFivePm() => new(17, 0);

    private static partial DateTime CombineDateAndTime(DateOnly d, TimeOnly t) => d.ToDateTime(t);

    private static partial DateOnly TruncateToDate(DateTimeOffset dt) => DateOnly.FromDateTime(dt.Date);

    private static partial DateOnly? ParseDateOnlyOrNull(string? s)
        => string.IsNullOrEmpty(s) ? null : DateOnly.Parse(s!, CultureInfo.InvariantCulture);

    private static partial Recurrence CreateRecurrence(
        string freq,
        int interval,
        List<TimeOnly> times,
        DateOnly? start,
        DateOnly? end,
        int? count)
        => new Recurrence(freq, interval, times, start, end, count);

    private static partial string FormatTimeTokenShim(TimeOnly t, bool friendly, string freq)
    {
        if (!friendly)
            return string.Equals(freq, "hour", StringComparison.OrdinalIgnoreCase) && t.Hour == 0
                ? t.Minute.ToString(CultureInfo.InvariantCulture)
                : t.ToString("HH\\:mm", CultureInfo.InvariantCulture);

        var h = t.Hour;
        var m = t.Minute;
        var isPm = h >= 12;
        var h12 = h % 12;
        if (h12 == 0) h12 = 12;
        return m == 0 ? $"{h12}{(isPm ? "p" : "a")}" : $"{h12}:{m:00}{(isPm ? "p" : "a")}";
    }

    private static partial string FormatDateOnly(DateOnly d)
        => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static partial int GetHour(TimeOnly t) => t.Hour;
    private static partial int GetMinute(TimeOnly t) => t.Minute;
}
#endif