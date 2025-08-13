using System.Globalization;
using System.Text.RegularExpressions;

namespace TaskDsl;

using static Parser;

public static class RecurrenceExtensions
{
    public static Dictionary<string, string> ToRRule(this Recurrence r)
    {
        if (r.IsEmpty) return new Dictionary<string, string>();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var freq = r.Freq switch
        {
            "min" => "MINUTELY",
            "hour" => "HOURLY",
            "day" => "DAILY",
            "week" => "WEEKLY",
            "month" => "MONTHLY",
            "year" => "YEARLY",
            _ => null // weekday / nth-weekday handled via BYDAY / POSTPOSE below
        };

        if (freq != null) map["FREQ"] = freq;
        if (r.Interval != 1) map["INTERVAL"] = r.Interval.ToString(CultureInfo.InvariantCulture);
        if (r.Count is { } c) map["COUNT"] = c.ToString(CultureInfo.InvariantCulture);
        if (r.End is { } e) map["UNTIL"] = e.ToString("yyyyMMdd") + "T235959Z";

        if (IsWeekday(r.Freq))
        {
            map["FREQ"] = "WEEKLY";
            map["BYDAY"] = DayToIcal(r.Freq);
        }
        else if (Regex.IsMatch(r.Freq, "^(?:[1-5]|last)(mon|tue|wed|thu|fri|sat|sun)$", RegexOptions.IgnoreCase))
        {
            map["FREQ"] = "MONTHLY";
            var m = Regex.Match(r.Freq, "^(?<n>[1-5]|last)(?<d>mon|tue|wed|thu|fri|sat|sun)$", RegexOptions.IgnoreCase);
            var setPos = m.Groups["n"].Value.Equals("last", StringComparison.OrdinalIgnoreCase) ? "-1" : m.Groups["n"].Value;
            map["BYDAY"] = DayToIcal(m.Groups["d"].Value);
            map["BYSETPOS"] = setPos;
        }

        if (r.Times.Count > 0)
        {
            map["BYHOUR"] = string.Join(",", r.Times.Select(t => t.Hour));
            map["BYMINUTE"] = string.Join(",", r.Times.Select(t => t.Minute));
        }

        return map;

        static string DayToIcal(string s) => s.ToLowerInvariant() switch
        {
            "mon" => "MO", "tue" => "TU", "wed" => "WE", "thu" => "TH",
            "fri" => "FR", "sat" => "SA", "sun" => "SU",
            _ => throw new ArgumentOutOfRangeException(nameof(s))
        };
    }
    
     public static string ToCronString(this Recurrence r)
    {
        if (r.IsEmpty)
            throw new ArgumentException("Recurrence is empty; cannot produce cron string.");

        string dayOfMonth = "*", month = "*", dayOfWeek = "*";

        // Build distinct, sorted lists for minutes/hours when times are present
        var minutes = r.Times.Count > 0
            ? r.Times.Select(t => t.Minute).Distinct().OrderBy(x => x).ToList()
            : [0];

        var hours = r.Times.Count > 0
            ? r.Times.Select(t => t.Hour).Distinct().OrderBy(x => x).ToList()
            : []; // empty means "*"

        // Defaults (used by day/week/month/weekday cases)
        var minute = string.Join(",", minutes);
        var hour = hours.Count > 0 ? string.Join(",", hours) : "*";

        switch (r.Freq)
        {
            case "min":
                minute = $"*/{r.Interval}";
                hour = "*";
                dayOfMonth = "*";
                month = "*";
                dayOfWeek = "*";
                break;

            case "hour":
                // For hourly, the minute list is meaningful; hours are */interval
                minute = string.Join(",", minutes);
                hour = $"*/{r.Interval}";
                dayOfMonth = "*";
                month = "*";
                dayOfWeek = "*";
                break;

            case "day":
                dayOfMonth = $"*/{r.Interval}";
                break;

            case "week":
                // Plain cron can’t do “every N weeks”; leave DOW="*"
                // If you need exact weekly days, use weekday freqs (mon..sun).
                break;

            case "month":
                month = $"*/{r.Interval}";
                break;

            case "year":
                // Cron can’t do */N years; pin to Jan 1 at given time(s)
                dayOfMonth = "1";
                month = "1";
                break;

            default:
                // Weekday mapping (0=Sun..6=Sat)
                var dowMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sun"] = "0", ["mon"] = "1", ["tue"] = "2", ["wed"] = "3",
                    ["thu"] = "4", ["fri"] = "5", ["sat"] = "6",
                };

                if (dowMap.TryGetValue(r.Freq, out var dow))
                {
                    dayOfWeek = dow;
                }
                else if (Regex.IsMatch(r.Freq, @"^(?:[1-5]|last)(mon|tue|wed|thu|fri|sat|sun)$", RegexOptions.IgnoreCase))
                {
                    throw new NotSupportedException($"Cron does not support nth/last weekday ({r.Freq}) directly.");
                }
                else
                {
                    throw new FormatException($"Unknown recurrence freq '{r.Freq}'.");
                }

                break;
        }

        return $"{minute} {hour} {dayOfMonth} {month} {dayOfWeek}";
    }
    
    
}