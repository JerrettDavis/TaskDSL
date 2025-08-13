namespace TaskDsl.Tests;

public class RoundTripTests
{
    private static readonly TimeZoneInfo Tz = TestUtil.ChicagoTz;
    private static readonly DateTimeOffset Now = TestUtil.FixedNowUtc;

    [Theory]
    // basic
    [InlineData("O [t1] -- Title")]
    // flags and numeric priority
    [InlineData("O [t2] ! ? p:2 -- Flags and priority")]
    // quoted tokens + escaped --
    [InlineData("O [t3] ^\"sam j\" -\"support team\" @\"HQ North\" -- Replace filters \\-- lobby")]
    // dependencies, tags, multiple assignees
    [InlineData("O [t4] ^jd ^sam -work -bgis +[root] +[t1] -- Deploy")]
    // due + estimate
    [InlineData("O [t5] >2025-09-01 =45m -- Due and estimate")]
    // weekday recurrence + times
    [InlineData("O [t6] *wed+8a+1p -- Standup")]
    // nth weekday
    [InlineData("O [t7] *1mon+2p -- CAB meeting")]
    // last weekday
    [InlineData("O [t8] *lastfri+17:00 -- Finance report")]
    // hourly with minute-only tokens
    [InlineData("O [t9] *hour/3+15+45 -- Polling")]
    // daily interval with two times + start/end
    [InlineData("O [t10] *day/2+08:00+20:00@2025-01-01~count:5 -- Health check")]
    // meta and contexts
    [InlineData("O [t11] @office meta:ticket=BG-42 meta:src=ops -- Work item")]
    // done item
    [InlineData("X [t12] -done -- Completed")]
    public void Dsl_ToString_And_Back_Preserves_Semantics(string original)
    {
        // Parse original
        var a = Parser.ParseLine(original, Tz, Now);

        // Render canonical DSL
        var canonical = a.ToString();

        // Parse canonical back
        var b = Parser.ParseLine(canonical, Tz, Now);

        AssertTasksEqual(a, b);
    }

    private static void AssertTasksEqual(TodoTask a, TodoTask b)
    {
        // Scalar fields
        Assert.Equal(a.Status, b.Status);
        Assert.Equal(a.Id, b.Id);
        Assert.Equal(a.Title, b.Title);
        Assert.Equal(a.Priority, b.Priority);
        Assert.Equal(a.BlockedExplicit, b.BlockedExplicit);

        // Sets / multisets (order-insensitive)
        Assert.True(a.Assignees.SetEquals(b.Assignees));
        Assert.True(a.Tags.SetEquals(b.Tags));
        Assert.Equal(
            a.Dependencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
            b.Dependencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        );
        Assert.True(a.Contexts.SetEquals(b.Contexts));
        Assert.Equal(
            a.Meta.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase),
            b.Meta.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
        );

        // Priority level, estimate, due
        Assert.Equal(a.PriorityLevel, b.PriorityLevel);
        Assert.Equal(a.Estimate, b.Estimate);

        // Due can be rendered as date-only; compare date when both present
        if (a.Due.HasValue && b.Due.HasValue)
        {
            Assert.Equal(a.Due.Value.Date, b.Due.Value.Date);
        }
        else
        {
            Assert.Equal(a.Due.HasValue, b.Due.HasValue);
        }

        // Recurrence (compare by printed canonical form to ignore ordering of times)
        var aRec = a.Recurrence.IsEmpty ? "" : a.Recurrence.ToString();
        var bRec = b.Recurrence.IsEmpty ? "" : b.Recurrence.ToString();
        Assert.Equal(aRec, bRec);

        // Times list equality when present
        if (!a.Recurrence.IsEmpty && !b.Recurrence.IsEmpty)
        {
            var aTimes = a.Recurrence.Times.OrderBy(t => t.Hour).ThenBy(t => t.Minute).ToArray();
            var bTimes = b.Recurrence.Times.OrderBy(t => t.Hour).ThenBy(t => t.Minute).ToArray();
            Assert.Equal(aTimes, bTimes);
            Assert.Equal(a.Recurrence.Interval, b.Recurrence.Interval);
            Assert.Equal(a.Recurrence.Freq, b.Recurrence.Freq, ignoreCase: true);
            Assert.Equal(a.Recurrence.Start, b.Recurrence.Start);
            Assert.Equal(a.Recurrence.End, b.Recurrence.End);
            Assert.Equal(a.Recurrence.Count, b.Recurrence.Count);
        }
    }
}