namespace TaskDsl.TaskAttributes;

public class TaskAttributeBuilder(TodoTask task)
{
    public string BuildCanonicalAttributes()
        => new AttributeBuilder()
            .AddIf(task.Priority, "!")
            .AddIf(task.BlockedExplicit, "?")
            .AddRangeOrdered(task.Assignees, a => NeedsQuoting(a) ? $"^\"{a}\"" : $"^{a}")
            .AddRangeOrdered(task.Tags, tag => NeedsQuoting(tag) ? $"#\"{tag}\"" : $"#{tag}")
            .AddRangeFluent(task.Dependencies.Select(dep => $"+[{dep}]"))
            .AddIf(!task.Recurrence.IsEmpty,() => $"*{task.Recurrence}")
            .AddIf(task.Due.HasValue, () => $">{task.Due?.ToString("yyyy-MM-dd")}")
            .AddIf(task.Estimate.HasValue, () => $"={FormatEstimate(task.Estimate!.Value)}")
            .AddIf(task.PriorityLevel.HasValue, () => $"p:{task.PriorityLevel!.Value}")
            .AddRangeOrdered(task.Contexts, ctx => NeedsQuoting(ctx) ? $"@\"{ctx}\"" : $"@{ctx}")
            .AddRangeOrderedKV(task.Meta, kv => $"meta:{kv.Key}={kv.Value}")
            .BuildCanonicalAttributes();

    private static bool NeedsQuoting(string s) =>
        s.Contains(' ') || s.Contains('\t') || s.Contains('"');

    private static string FormatEstimate(TimeSpan ts) =>
        ts.TotalDays >= 1 ? $"{(int)ts.TotalDays}d"
        : ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h"
        : $"{(int)ts.TotalMinutes}m";
}