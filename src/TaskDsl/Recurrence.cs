namespace TaskDsl;

#if NETSTANDARD1_1_OR_GREATER
public sealed record Recurrence(
    string Freq,          // "day","week","month","mon","1mon","lastfri", etc.
    int Interval,         // default 1
    List<DateTime> Times, // optional specific times per occurrence
    DateTime? Start,      // @YYYY-MM-DD
    DateTime? End,        // ~YYYY-MM-DD
    int? Count            // ~count:10
)
{
    public static Recurrence Empty => new("none", 1, [], null, null, null);
    public bool IsEmpty => Freq == "none";
}
#else
public sealed record Recurrence(
    string Freq,          // "day","week","month","mon","1mon","lastfri", etc.
    int Interval,         // default 1
    List<TimeOnly> Times, // optional specific times per occurrence
    DateOnly? Start,      // @YYYY-MM-DD
    DateOnly? End,        // ~YYYY-MM-DD
    int? Count            // ~count:10
)
{
    public static Recurrence Empty => new("none", 1, [], null, null, null);
    public bool IsEmpty => Freq == "none";
}
#endif