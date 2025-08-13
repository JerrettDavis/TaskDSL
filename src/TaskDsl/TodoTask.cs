namespace TaskDsl;

using static Parser;

public sealed record TodoTask
{
    public TaskStatus Status { get; init; }
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";

    // NEW flags
    public bool Priority { get; set; }        // set by '!' token
    public bool BlockedExplicit { get; set; } // set by '?' token

    // existing collections...
    public HashSet<string> Assignees { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Tags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Dependencies { get; } = new();
    public Recurrence Recurrence { get; set; } = Recurrence.Empty;
    public DateTimeOffset? Due { get; set; }
    public TimeSpan? Estimate { get; set; }
    public int? PriorityLevel { get; set; } // keep p:1..n if you want both binary + numeric
    public HashSet<string> Contexts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Meta { get; } = new(StringComparer.OrdinalIgnoreCase);


    public override string ToString()
    {
        var parts = new List<string>
        {
            // Status
            Status == TaskStatus.Done ? "X" : "O",
            // ID
            $"[{Id}]"
        };

        // Flags
        if (Priority) parts.Add("!");
        if (BlockedExplicit) parts.Add("?");

        // Assignees
        parts
            .AddRange(Assignees.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(a => NeedsQuoting(a) ? $"^\"{a}\"" : $"^{a}"));

        // Tags
        parts
            .AddRange(Tags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(tag => NeedsQuoting(tag) ? $"#\"{tag}\"" : $"#{tag}"));

        // Dependencies
        parts.AddRange(Dependencies.Select(dep => $"+[{dep}]"));

        // Recurrence
        if (!Recurrence.IsEmpty)
            parts.Add($"*{RecurrenceToString(Recurrence)}");

        // Due
        if (Due.HasValue)
            parts.Add($">{Due.Value:yyyy-MM-dd}");

        // Estimate
        if (Estimate.HasValue)
            parts.Add($"={FormatEstimate(Estimate.Value)}");

        // Numeric priority
        if (PriorityLevel.HasValue)
            parts.Add($"p:{PriorityLevel.Value}");

        // Contexts
        parts.AddRange(Contexts.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(ctx => NeedsQuoting(ctx) ? $"@\"{ctx}\"" : $"@{ctx}"));

        // Meta
        parts.AddRange(Meta.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"meta:{kv.Key}={kv.Value}"));

        // Title
        var titlePart = string.IsNullOrWhiteSpace(Title) ? "" : $" -- {EscapeTitle(Title)}";

        return string.Join(" ", parts) + titlePart;
    }

    private static bool NeedsQuoting(string s) =>
        s.Contains(' ') || s.Contains('\t') || s.Contains('"');

    private static string EscapeTitle(string title) =>
        title.Replace("--", "\\--");


    public string ToBulletString()
    {
        // Only if it's a simple ad-hoc shape: no recurrence/due/meta/etc.
        var simple = Recurrence.IsEmpty && 
                     !Due.HasValue && 
                     !Estimate.HasValue &&
                     !Priority && 
                     !BlockedExplicit && 
                     !PriorityLevel.HasValue &&
                     Contexts.Count == 0 && 
                     Meta.Count == 0;

        if (!simple) return ToString(); // fall back to full DSL
        var parts = new List<string> { Status == TaskStatus.Done ? "~~" : "-" };

        // Compose inline markers
        var text = Title;
        if (Assignees.Count > 0) text += " " + string.Join(" ", Assignees.Select(a => "@" + a));
        if (Tags.Count > 0) text += " " + string.Join(" ", Tags.Select(t => "#" + t));
        if (Dependencies.Count > 0) text += " " + string.Join(" ", Dependencies.Select(d => $"+[{d}]"));

        return $"{parts[0]} {text}".TrimEnd();
    }

    public string ToPrettyString()
    {
        var statusIcon = Status == TaskStatus.Done ? "[✓]" : "[ ]";
        if (BlockedExplicit) statusIcon = "[✗]";
        if (Priority) statusIcon += "★";

        // Compose detail pills
        var parts = new List<string>();

        if (Tags.Count > 0)
            parts.Add("Tags: " + string.Join(", ",
                Tags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(t => "#" + t)));

        if (Assignees.Count > 0)
            parts.Add(string.Join(", ", Assignees
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(a => $"@{a}")));

        if (Contexts.Count > 0)
            parts.Add(string.Join(", ", Contexts
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(c => $"@{c}")));

        if (Due.HasValue)
            parts.Add($"Due: {Due.Value:yyyy-MM-dd}");

        if (!Recurrence.IsEmpty)
            parts.Add($"Repeat: {RecurrenceToString(Recurrence, friendlyTimes: true)}");

        if (Dependencies.Count > 0)
            parts.Add("After: " + string.Join(", ", Dependencies));

        if (Estimate.HasValue)
            parts.Add($"Est: {FormatEstimate(Estimate.Value)}");

        if (PriorityLevel.HasValue)
            parts.Add($"P{PriorityLevel.Value}");

        if (Meta.Count > 0)
            parts.Add("Meta: " + string.Join(", ", Meta
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key}={kv.Value}")));

        var details = parts.Count == 0 ? "" : "\n    " + string.Join(" | ", parts);

        return $"{statusIcon} {Title} ({Id}){details}";
    }

    private static string FormatEstimate(TimeSpan ts) =>
        ts.TotalDays >= 1 ? $"{(int)ts.TotalDays}d"
        : ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h"
        : $"{(int)ts.TotalMinutes}m";
}