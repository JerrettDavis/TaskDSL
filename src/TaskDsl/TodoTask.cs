using TaskDsl.TaskAttributes;

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
        var attrBuilder = new TaskAttributeBuilder(this);
        var attr = attrBuilder.BuildCanonicalAttributes();
        if (!string.IsNullOrWhiteSpace(attr))
            parts.Add(attr);
        var titlePart = string.IsNullOrWhiteSpace(Title) ? "" : $" -- {EscapeTitle(Title)}";
        return string.Join(" ", parts) + titlePart;
    }

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

        var pills = new AttributeBuilder()
            .AddIf(Tags.Count > 0, () => "Tags: " + string.Join(", ", Tags.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(t => "#" + t)))
            .AddIf(Assignees.Count > 0, () => string.Join(", ", Assignees.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(a => $"@{a}")))
            .AddIf(Contexts.Count > 0, () => string.Join(", ", Contexts.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(c => $"@{c}")))
            .AddIf(Due.HasValue, () => $"Due: {Due!.Value:yyyy-MM-dd}")
            .AddIf(!Recurrence.IsEmpty, () => $"Repeat: {Recurrence.ToString(friendlyTimes: true)}")
            .AddIf(Dependencies.Count > 0, () => "After: " + string.Join(", ", Dependencies))
            .AddIf(Estimate.HasValue, () => $"Est: {FormatEstimate(Estimate!.Value)}")
            .AddIf(PriorityLevel.HasValue, () => $"P{PriorityLevel!.Value}")
            .AddIf(Meta.Count > 0, () => "Meta: " + string.Join(", ", Meta.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).Select(kv => $"{kv.Key}={kv.Value}")));

        var details = pills.AttributeCount == 0 ? "" : "\n    " + pills.BuildCanonicalAttributes(" | ");
        return $"{statusIcon} {Title} ({Id}){details}";
    }

    private static string FormatEstimate(TimeSpan ts) =>
        ts.TotalDays >= 1 ? $"{(int)ts.TotalDays}d"
        : ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h"
        : $"{(int)ts.TotalMinutes}m";
}