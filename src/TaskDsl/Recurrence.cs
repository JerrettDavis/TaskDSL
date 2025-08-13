using System.Globalization;

namespace TaskDsl;

using static Parser;

public sealed record Recurrence(
    string Freq,  // "day","week","month","mon","1mon","lastfri", etc.
    int Interval, // default 1
#if NETSTANDARD1_1_OR_GREATER

    List<DateTime> Times, // optional specific times per occurrence
    DateTime? Start,      // @YYYY-MM-DD
    DateTime? End,        // ~YYYY-MM-DD
#else

    List<TimeOnly> Times, // optional specific times per occurrence
    DateOnly? Start,      // @YYYY-MM-DD
    DateOnly? End,        // ~YYYY-MM-DD

#endif
    int? Count            // ~count:10
)
{
    public static Recurrence Empty => new("none", 1, [], null, null, null);
    public bool IsEmpty => Freq == "none";

    public string ToString(bool friendlyTimes)
    {
        if (IsEmpty) return string.Empty;

        var parts = new List<string>();

        // freq + optional /interval
        var head = Freq;
        if (Interval != 1) head += "/" + Interval;
        parts.Add(head);

        // +times (sorted, distinct)
        if (Times is { Count: > 0 })
        {
            parts.AddRange(Times
                .Distinct()
                .OrderBy(GetHour)
                .ThenBy(GetMinute)
                .Select(t => "+" + FormatTimeTokenShim(t, friendlyTimes, Freq)));
        }

        if (Start is { } s) parts.Add("@" + FormatDateOnly(s));
        if (End is { } e) parts.Add("~" + FormatDateOnly(e));
        else if (Count is { } c) parts.Add("~count:" + c.ToString(CultureInfo.InvariantCulture));

        return string.Concat(parts);
    }

    public override string ToString()
        => ToString(false);
}