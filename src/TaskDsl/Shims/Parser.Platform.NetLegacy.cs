using System.Globalization;
using System.Text.RegularExpressions;

#if !NET6_0_OR_GREATER
namespace TaskDsl;

public static partial class Parser
{
    private static partial DateTime NextOrSameDate(DateTimeOffset localNow, DayOfWeek target, bool inclusive)
    {
        var start = localNow.Date;
        var delta = ((int)target - (int)start.DayOfWeek + 7) % 7;
        if (delta == 0 && !inclusive) delta = 7;
        return start.AddDays(delta);
    }
    
    private static partial string HexLower6(byte[] bytes)
    {
        // BitConverter yields "AA-BB-..." → strip dashes, lower, take 6
        var hex = BitConverter.ToString(bytes).Replace("-", "");
        // Avoid extra allocs if you like, but this keeps it simple/readable:
        return hex.ToLowerInvariant()[..6];
    }

    private static partial DateTime ParseTimeToken(string s)
    {
        s = s.Trim().ToLowerInvariant();

        if (Regex.IsMatch(s, @"^\d{1,2}$"))
        {
            var m = int.Parse(s);
            if (m is < 0 or > 59) throw new FormatException("Minute token must be 0..59.");
            return new DateTime(1, 1, 1, 0, m, 0);
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
            return new DateTime(1, 1, 1, h, m, 0);
        }

        // Accepts "HH:mm" or "H:mm"
        return DateTime.ParseExact(s, "HH:mm", CultureInfo.InvariantCulture);
    }

    private static partial DateTime DefaultFivePm() => new DateTime(1, 1, 1, 17, 0, 0);

    private static partial DateTime CombineDateAndTime(DateTime d, DateTime t)
        => new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, 0);

    private static partial DateTime TruncateToDate(DateTimeOffset dt) => dt.Date;

    private static partial DateTime? ParseDateOnlyOrNull(string? s)
        => string.IsNullOrEmpty(s) ? (DateTime?)null : DateTime.Parse(s!, CultureInfo.InvariantCulture).Date;

    private static partial Recurrence CreateRecurrence(
        string freq, int interval, List<DateTime> times, DateTime? start, DateTime? end, int? count)
        => new Recurrence(freq, interval, times, start, end, count);

    private static partial string FormatTimeTokenShim(DateTime t, bool friendly, string freq)
    {
        if (!friendly)
            return string.Equals(freq, "hour", StringComparison.OrdinalIgnoreCase) && t.Hour == 0
                ? t.Minute.ToString(CultureInfo.InvariantCulture)
                : t.ToString("HH\\:mm", CultureInfo.InvariantCulture);

        var h = t.Hour;
        var m = t.Minute;
        var isPm = h >= 12;
        var h12 = h % 12; if (h12 == 0) h12 = 12;
        return m == 0 ? $"{h12}{(isPm ? "p" : "a")}" : $"{h12}:{m:00}{(isPm ? "p" : "a")}";
    }

    private static partial string FormatDateOnly(DateTime d)
        => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static partial int GetHour(DateTime t) => t.Hour;
    private static partial int GetMinute(DateTime t) => t.Minute;
}
#endif
