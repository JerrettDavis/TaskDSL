using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TaskDsl;

public static partial class Parser
{
    private static readonly HashSet<char> SigilsNeedingQuoteJoin = ['^', '#', '-', '@'];
    private const string IdPattern = "^[A-Za-z0-9_-]+$";

    private static string StripOuterQuotes(string v)
        => v.Length >= 2 &&
           ((v[0] == '"' && v[^1] == '"') ||
            (v[0] == '\'' && v[^1] == '\''))
            ? Regex.Unescape(v[1..^1])
            : v;

    private static string ValueAfterSigil(string t) => StripOuterQuotes(t[1..]);

    private static bool IsValidId(string id) => Regex.IsMatch(id, IdPattern);

    private static string RequireValidId(string id, string errorPrefix)
    {
        if (!IsValidId(id)) throw new FormatException($"{errorPrefix}: '{id}'");
        return id;
    }

// Weekdays / nth-weekday helpers
    private static readonly HashSet<string> Weekdays = new(StringComparer.OrdinalIgnoreCase)
        { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

    private static bool IsWeekday(string s) => Weekdays.Contains(s);

    private static bool IsNthWeekday(string s) =>
        Regex.IsMatch(s, "^(?:[1-5]|last)(mon|tue|wed|thu|fri|sat|sun)$", RegexOptions.IgnoreCase);


    public static TodoTask ParseLine(string line, TimeZoneInfo tz, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(line)) throw new ArgumentException("Empty line.");
        var trimmed = line.TrimStart();
        // 1) Bullet mode – open ad-hoc
        if (trimmed.StartsWith("- "))
            return ParseBullet(trimmed[2..], isDone: false);

        // 2) Bullet mode – done ad-hoc
        if (trimmed.StartsWith("~~ "))
            return ParseBullet(trimmed[3..], isDone: true);

        // 3) Otherwise, normal DSL
        var cut = line.IndexOf(" -- ", StringComparison.Ordinal);
        var left = cut >= 0 ? line[..cut] : line;
        var title = cut >= 0 ? line[(cut + 4)..].Trim() : "";

        title = title.Replace(@"\--", "--");


        var tokens = TokenizeRespectingQuotedSigils(left);
        if (tokens.Count < 2) throw new FormatException("Expected at least <status> and <[id]>.");
        var id = ParseId(tokens[1]);
        var statusToken = tokens[0];
        var status = ParseStatus(statusToken);
        var task = new TodoTask { Status = status, Id = id, Title = title };
        if (statusToken.Equals("O!", StringComparison.OrdinalIgnoreCase))
            task.Priority = true;

        for (var i = 2; i < tokens.Count; i++)
        {
            var t = tokens[i];

            // Simple flags
            if (t == "!")
            {
                task.Priority = true;
                continue;
            }

            if (t == "?")
            {
                task.BlockedExplicit = true;
                continue;
            }

            // By leading char
            switch (t[0])
            {
                case '^':
                    task.Assignees.Add(ValueAfterSigil(t));
                    break;

                case '#': // primary tag
                case '-': // legacy tag
                    task.Tags.Add(ValueAfterSigil(t));
                    break;

                case '@':
                    task.Contexts.Add(ValueAfterSigil(t));
                    break;

                case '+':
                    if (t is [_, '[', _, _, ..] && t[^1] == ']')
                        task.Dependencies.Add(ExtractIdFromBracket(t));
                    else
                        throw new FormatException($"Bad dependency token '{t}'. Use +[id].");
                    break;

                case '*':
                    task.Recurrence = ParseRecurrence(t[1..]);
                    break;

                case '>':
                    task.Due = ParseDue(t[1..], tz, now);
                    break;

                case '=':
                    task.Estimate = ParseDuration(t[1..]);
                    break;

                default:
                    if (t.StartsWith("p:", StringComparison.OrdinalIgnoreCase))
                        task.PriorityLevel = int.Parse(t.AsSpan(2));
                    else if (t.StartsWith("meta:", StringComparison.OrdinalIgnoreCase))
                        ParseMeta(task, t[5..]);
                    else
                        throw new FormatException($"Unknown token '{t}'.");
                    break;
            }
        }

        return task;
    }


    private static TodoTask ParseBullet(string text, bool isDone)
    {
        // Extract inline tags and assignees (hashtags and @mentions)
        var tags = ExtractHashTags(text);
        var assignees = ExtractMentions(text);
        var deps = ExtractDeps(text); // supports +[id] or "+ [id]"

        // Clean title (strip inline markers but keep readable text)
        var title = CleanBulletTitle(text);

        var id = GenerateIdFromText(title);

        var t = new TodoTask
        {
            Status = isDone ? TaskStatus.Done : TaskStatus.Open,
            Id = id,
            Title = title
        };
        foreach (var tag in tags) t.Tags.Add(tag);
        foreach (var a in assignees) t.Assignees.Add(a);
        t.Dependencies.AddRange(deps);

        return t;
    }

    private static IEnumerable<string> ExtractHashTags(string s) =>
        Regex.Matches(s, @"(?<=^|\s)#([A-Za-z0-9_-]+)")
            .Select(m => m.Groups[1].Value);

    private static IEnumerable<string> ExtractMentions(string s) =>
        Regex.Matches(s, @"(?<=^|\s)@([A-Za-z0-9_-]+)")
            .Select(m => m.Groups[1].Value);

    private static IEnumerable<string> ExtractDeps(string s) =>
        Regex.Matches(s, @"\+\s*\[([A-Za-z0-9_-]+)\]")
            .Select(m => m.Groups[1].Value);

    private static string CleanBulletTitle(string s)
    {
        // Remove #tags, @mentions, +[id], and extra spaces
        var cleaned = Regex.Replace(s,
            @"(?<=^|\s)(#[A-Za-z0-9_-]+|@[A-Za-z0-9_-]+|\+\s*\[[A-Za-z0-9_-]+\])",
            "").Trim();
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
        return cleaned;
    }

    private static string GenerateIdFromText(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "note-" + Guid.NewGuid().ToString("N")[..6];

        // slug
        var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (slug.Length > 32) slug = slug[..32].Trim('-');

        // tiny hash for stability (not for security)
        using var sha = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(title);
        var hash6 = HexLower6(sha.ComputeHash(bytes)); // << shim call

        return $"{slug}-{hash6}";
    }

    private static partial string HexLower6(byte[] bytes);


    private static TaskStatus ParseStatus(string s) => s.ToUpperInvariant() switch
    {
        "O" => TaskStatus.Open,
        "X" => TaskStatus.Done,
        // Back-compat: "O!" -> Open + priority flag. We’ll set the flag when scanning attributes.
        "O!" => TaskStatus.Open,
        _ => throw new FormatException($"Invalid status '{s}'. Use 'O' or 'X'.")
    };


    private static string ParseId(string token)
    {
        if (!token.StartsWith('[') || !token.EndsWith(']'))
            throw new FormatException("ID must be [slug].");
        var id = token[1..^1];
        return RequireValidId(id, "ID may contain only A-Z a-z 0-9 _ -");
    }

    private static string ExtractIdFromBracket(string t)
    {
        var id = t[2..^1];
        return RequireValidId(id, "Dependency id invalid");
    }


    private static void ParseMeta(TodoTask task, string kv)
    {
        var idx = kv.IndexOf('=');
        if (idx <= 0 || idx == kv.Length - 1) throw new FormatException($"Bad meta token 'meta:{kv}'");
        var k = kv[..idx];
        var v = kv[(idx + 1)..];
        task.Meta[k] = v;
    }

    // ---- Due parsing: >2025-08-20 or >fri+5p or >14:30 (today) ----
    private static DateTimeOffset ParseDue(string token, TimeZoneInfo tz, DateTimeOffset now)
    {
        if (DateTimeOffset.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
            return dt;

        var parts = token.Split('+', 2, StringSplitOptions.RemoveEmptyEntries);
        var localNow = TimeZoneInfo.ConvertTime(now, tz);

        if (parts.Length >= 1 && TryParseWeekday(parts[0], out var targetDow))
        {
            var date = NextOrSameDate(localNow, targetDow, inclusive: localNow.TimeOfDay <= TimeSpan.Zero);
            var t = parts.Length == 2 ? ParseTimeToken(parts[1]) : DefaultFivePm();
            var local = CombineDateAndTime(date, t);
            return TimeZoneInfo.ConvertTimeToUtc(local, tz);
        }

        // time-only => today at that time (or next day if past)
        var timeOnly = ParseTimeToken(token);
        var today = TruncateToDate(localNow);
        var local2 = CombineDateAndTime(today, timeOnly);
        if (local2 <= localNow) local2 = local2.AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(local2, tz);
    }

    private static bool TryParseWeekday(string s, out DayOfWeek dow)
    {
        dow = s.ToLowerInvariant() switch
        {
            "mon" => DayOfWeek.Monday,
            "tue" or "tues" => DayOfWeek.Tuesday,
            "wed" => DayOfWeek.Wednesday,
            "thu" or "thur" or "thurs" => DayOfWeek.Thursday,
            "fri" => DayOfWeek.Friday,
            "sat" => DayOfWeek.Saturday,
            "sun" => DayOfWeek.Sunday,
            _ => (DayOfWeek)(-1)
        };
        return (int)dow >= 0;
    }

    private static TimeSpan ParseDuration(string s)
    {
        // 45m, 2h, 3d
        var m = Regex.Match(s, @"^(?<n>\d+)(?<u>[mhd])$", RegexOptions.IgnoreCase);
        if (!m.Success) throw new FormatException("Bad duration. Use 30m|2h|3d.");
        var n = int.Parse(m.Groups["n"].Value);
        return char.ToLowerInvariant(m.Groups["u"].Value[0]) switch
        {
            'm' => TimeSpan.FromMinutes(n),
            'h' => TimeSpan.FromHours(n),
            'd' => TimeSpan.FromDays(n),
            _ => throw new FormatException("Bad duration unit.")
        };
    }

    public static bool IsBlocked(TodoTask task, IReadOnlyDictionary<string, TodoTask> byId)
    {
        if (task.BlockedExplicit) return true;
        // Blocked by any dependency not done
        foreach (var depId in task.Dependencies)
        {
            if (!byId.TryGetValue(depId, out var dep)) return true; // missing dep → conservatively blocked
            if (dep.Status != TaskStatus.Done) return true;
        }

        return false;
    }

    public static (bool Blocked, string? Reason) ComputeBlockState(TodoTask task, IReadOnlyDictionary<string, TodoTask> byId)
    {
        if (task.BlockedExplicit) return (true, "explicit");
        foreach (var depId in task.Dependencies)
        {
            if (!byId.TryGetValue(depId, out var dep)) return (true, $"missing dependency [{depId}]");
            if (dep.Status != TaskStatus.Done) return (true, $"waiting on [{depId}]");
        }

        return (false, null);
    }

    // ---- Recurrence parsing: *<freq>[/<interval>][+<time>][@<start>][~<end>|~count:N] ----
    public static Recurrence ParseRecurrence(string r)
    {
        // split on +, @, ~ while keeping the main freq/interval prefix
        // Example: "mon/2+2p+10:30@2025-01-01~2025-12-31"
        var freqAndInterval = r;
        string? start = null, end = null;
        int? count = null;

        // Extract @start and ~end|count first
        var atIdx = r.IndexOf('@');
        if (atIdx >= 0)
        {
            start = r[(atIdx + 1)..];
            freqAndInterval = r[..atIdx];
        }

        var tilIdx = (start ?? r).IndexOf('~'); // search original string to preserve positions
        if (tilIdx >= 0)
        {
            var src = start == null ? r : r[(atIdx + 1)..];
            var before = src[..tilIdx];
            var after = src[(tilIdx + 1)..];
            // put back together: freqAndInterval may have lost tail; rebuild:
            if (start != null) start = before;
            else freqAndInterval = before;

            if (after.StartsWith("count:", StringComparison.OrdinalIgnoreCase))
                count = int.Parse(after[6..]);
            else
                end = after;
        }

        // Times: +t segments
        var timeParts = freqAndInterval.Split('+', StringSplitOptions.RemoveEmptyEntries);
        var basePart = timeParts[0];
        var times = timeParts.Skip(1).Select(ParseTimeToken).ToList(); // platform-neutral


        // Interval
        var baseBits = basePart.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        var freq = baseBits[0].ToLowerInvariant();
        var interval = baseBits.Length == 2 ? int.Parse(baseBits[1]) : 1;

        if (interval < 1)
            throw new FormatException("Recurrence interval must be >= 1.");

        ValidateFreq(freq);

        var startDate = ParseDateOnlyOrNull(start);
        var endDate = ParseDateOnlyOrNull(end);

        return CreateRecurrence(freq, interval, times, startDate, endDate, count); // platform-neutral ctor
    }


    private static List<string> TokenizeRespectingQuotedSigils(string left)
    {
        var regex = new Regex("""
                                      (?<dq>"(?:[^"\\]|\\.)*")         # double-quoted
                                      | (?<sq>'(?:[^'\\]|\\.)*')          # single-quoted
                                      | (?<plain>\S+)                     # plain
                              """,
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);
        var raw = regex.Matches(left).Select(m => m.Value).ToList();
        var outTokens = new List<string>(raw.Count);

        for (var i = 0; i < raw.Count; i++)
        {
            var tok = raw[i];

            // If the token starts with a known sigil and an opening quote, join until closing quote
            var startsQuotedWithSigil =
                tok.Length >= 2 && SigilsNeedingQuoteJoin.Contains(tok[0]) && tok[1] == '"';

            if (startsQuotedWithSigil && !tok.EndsWith('"'))
            {
                var acc = tok;
                while (i + 1 < raw.Count)
                {
                    i++;
                    acc += " " + raw[i];
                    if (raw[i].EndsWith('"')) break;
                }

                outTokens.Add(acc);
            }
            else
            {
                outTokens.Add(tok);
            }
        }

        // Now unquote bare-quoted tokens overall (but keep sigil-aware quotes for per-attribute parsing)
        return outTokens.Select(Unquote).ToList();
    }

    private static string Unquote(string token) => StripOuterQuotes(token);


    private static void ValidateFreq(string f)
    {
        f = f.Trim().ToLowerInvariant();

        // Allowed: unit freqs, weekdays, nth/last weekdays
        var isUnit = f is "min" or "hour" or "day" or "week" or "month" or "year";

        if (!(isUnit || IsWeekday(f) || IsNthWeekday(f)))
            throw new FormatException($"Bad recurrence freq '{f}'.");
    }


    public static Dictionary<string, string> ToRRule(Recurrence r)
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

    public static string ToCronString(Recurrence r)
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


    public static string RecurrenceToString(Recurrence r, bool friendlyTimes = false)
    {
        if (r is null) throw new ArgumentNullException(nameof(r));
        if (r.IsEmpty) return string.Empty;

        var parts = new List<string>();

        // freq + optional /interval
        var head = r.Freq;
        if (r.Interval != 1) head += "/" + r.Interval;
        parts.Add(head);

        // +times (sorted, distinct)
        if (r.Times is { Count: > 0 })
        {
            parts.AddRange(r.Times
                .Distinct()
                .OrderBy(GetHour)
                .ThenBy(GetMinute)
                .Select(t => "+" + FormatTimeTokenShim(t, friendlyTimes, r.Freq)));
        }

        if (r.Start is { } s) parts.Add("@" + FormatDateOnly(s));
        if (r.End is { } e) parts.Add("~" + FormatDateOnly(e));
        else if (r.Count is { } c) parts.Add("~count:" + c.ToString(CultureInfo.InvariantCulture));

        return string.Concat(parts);
    }


#if NET6_0_OR_GREATER
    // Native modern types
    private static partial DateOnly NextOrSameDate(DateTimeOffset localNow, DayOfWeek target, bool inclusive);
    private static partial TimeOnly ParseTimeToken(string s);
    private static partial TimeOnly DefaultFivePm();
    private static partial DateTime CombineDateAndTime(DateOnly d, TimeOnly t);
    private static partial DateOnly TruncateToDate(DateTimeOffset dt);
    private static partial DateOnly? ParseDateOnlyOrNull(string? s);
    private static partial Recurrence CreateRecurrence(string freq, int interval, List<TimeOnly> times, DateOnly? start, DateOnly? end, int? count);
    private static partial string FormatTimeTokenShim(TimeOnly t, bool friendly, string freq);
    private static partial string FormatDateOnly(DateOnly d);
    private static partial int GetHour(TimeOnly t);
    private static partial int GetMinute(TimeOnly t);

#else
    // Legacy types (“date-only” as DateTime.Date, “time-only” as DateTime at 0001-01-01)
    private static partial DateTime  NextOrSameDate(DateTimeOffset localNow, DayOfWeek target, bool inclusive);
    private static partial DateTime  ParseTimeToken(string s);
    private static partial DateTime  DefaultFivePm();
    private static partial DateTime  CombineDateAndTime(DateTime d, DateTime t);
    private static partial DateTime  TruncateToDate(DateTimeOffset dt);
    private static partial DateTime? ParseDateOnlyOrNull(string? s);
    private static partial Recurrence CreateRecurrence(string freq, int interval, List<DateTime> times, DateTime? start, DateTime? end, int? count);
    private static partial string    FormatTimeTokenShim(DateTime t, bool friendly, string freq);
    private static partial string    FormatDateOnly(DateTime d);
    private static partial int       GetHour(DateTime t);
    private static partial int       GetMinute(DateTime t);
#endif
}