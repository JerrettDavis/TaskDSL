// File: TaskDsl.Tests.cs

namespace TaskDsl.Tests;

public class TaskDslTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById(
#if WINDOWS
        "Central Standard Time"   // America/Chicago on Windows
#else
        "America/Chicago"
#endif
    );

    [Fact]
    public void Parses_Simple_Task_With_Tags_Assignee_Title()
    {
        const string line = "O [work1] ^jd -work -bgis -- Clean up go-live checklist";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);

        var t = Parser.ParseLine(line, Tz, now);

        Assert.Equal(TaskStatus.Open, t.Status);
        Assert.Equal("work1", t.Id);
        Assert.Contains("jd", t.Assignees);
        Assert.Contains("work", t.Tags);
        Assert.Contains("bgis", t.Tags);
        Assert.Equal("Clean up go-live checklist", t.Title);
    }

    [Fact]
    public void Parses_Dependency_And_Recurrence()
    {
        const string line = "O [work2] ^jd +[work1] -work -bgis *mon+2p -- Send go-live checklist";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);

        var t = Parser.ParseLine(line, Tz, now);

        Assert.Equal("work2", t.Id);
        Assert.Contains("work1", t.Dependencies);
        Assert.Equal("mon", t.Recurrence.Freq);
        Assert.Single(t.Recurrence.Times);
        Assert.Equal(14, t.Recurrence.Times[0].Hour);
        Assert.Equal(0, t.Recurrence.Times[0].Minute);

        var rrule = Parser.ToRRule(t.Recurrence);
        Assert.Equal("WEEKLY", rrule["FREQ"]);
        Assert.Equal("MO", rrule["BYDAY"]);
        Assert.Equal("14", rrule["BYHOUR"]);
        Assert.Equal("0", rrule["BYMINUTE"]);
    }

    [Theory]
    [InlineData("*day/2")]
    [InlineData("*1mon+2p")]
    [InlineData("*lastfri+17:00")]
    [InlineData("*month/3+09:00@2025-01-01~2025-12-31")]
    public void Parses_Recurrence_Shapes(string recur)
    {
        var line = $"O [r1] {recur} -- Demo";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);
        var t = Parser.ParseLine(line, Tz, now);
        Assert.False(t.Recurrence.IsEmpty);
    }

    [Theory]
    [InlineData("O work1 -- missing brackets")]
    [InlineData("Z [bad] -- bad status")]
    [InlineData("O [bad id] -- spaces")]
    public void Rejects_Bad_Inputs(string line)
    {
        var now = DateTimeOffset.UtcNow;
        Assert.ThrowsAny<Exception>(() => Parser.ParseLine(line, Tz, now));
    }
    
    
    [Fact]
    public void Priority_Flag_From_Bang_Token()
    {
        var t = Parser.ParseLine("O [a] ! -- title", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Priority);
        Assert.Equal(TaskStatus.Open, t.Status);
    }

    [Fact]
    public void Priority_Flag_From_OBang_BackCompat()
    {
        var t = Parser.ParseLine("O! [a] -- title", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Priority);
        Assert.Equal(TaskStatus.Open, t.Status);
    }

    [Fact]
    public void Blocked_Computed_From_Dependencies()
    {
        var a = Parser.ParseLine("X [a] -- done", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var b = Parser.ParseLine("O [b] +[a] -- ok", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var c = Parser.ParseLine("O [c] +[b] -- blocked", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);

        var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = a, ["b"] = b, ["c"] = c
        };

        Assert.False(Parser.IsBlocked(b, map)); // a is done
        Assert.True(Parser.IsBlocked(c, map));  // b is open
    }

    [Fact]
    public void Blocked_Explicit_Overrides()
    {
        var t = Parser.ParseLine("O [x] ? -- explicit block", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var map = new Dictionary<string, TodoTask> { ["x"] = t };
        var (blocked, reason) = Parser.ComputeBlockState(t, map);
        Assert.True(blocked);
        Assert.Equal("explicit", reason);
    }
    
    [Fact]
    public void Recurrence_ToString_Canonical_And_Friendly()
    {
        var r = Parser.ParseRecurrence("day/2+8a+8p@2025-01-01~count:5");
        Assert.Equal("day/2+08:00+20:00@2025-01-01~count:5", Parser.RecurrenceToString(r));
        Assert.Equal("day/2+8a+8p@2025-01-01~count:5", Parser.RecurrenceToString(r, friendlyTimes:true));
    }

    [Fact]
    public void Recurrence_ToString_Hour_MinuteOnly_Roundtrip()
    {
        var r = Parser.ParseRecurrence("hour/3+15+45");
        Assert.Equal("hour/3+15+45", Parser.RecurrenceToString(r)); // minute-only preserved
    }
}