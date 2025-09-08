using TaskDsl.Extensions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class TaskDslTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById(
#if WINDOWS
        "Central Standard Time"   // America/Chicago on Windows
#else
        "America/Chicago"
#endif
    );

    [Fact]
    public Task Parses_Simple_Task_With_Tags_Assignee_Title()
    {
        const string line = "O [work1] ^jd -work -bgis -- Clean up go-live checklist";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);

        return Given(() => (line, now))
            .When(t => Parser.ParseLine(t.line, Tz, t.now))
            .Then(t =>
            {
                Assert.Equal(TaskStatus.Open, t.Status);
                Assert.Equal("work1", t.Id);
                Assert.Contains("jd", t.Assignees);
                Assert.Contains("work", t.Tags);
                Assert.Contains("bgis", t.Tags);
                Assert.Equal("Clean up go-live checklist", t.Title);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Parses_Dependency_And_Recurrence()
    {
        const string line = "O [work2] ^jd +[work1] -work -bgis *mon+2p -- Send go-live checklist";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);

        return Given(() => (line, now))
            .When(t => Parser.ParseLine(t.line, Tz, t.now))
            .Then(t =>
            {
                Assert.Equal("work2", t.Id);
                Assert.Contains("work1", t.Dependencies);
                Assert.Equal("mon", t.Recurrence.Freq);
                Assert.Single(t.Recurrence.Times);
                Assert.Equal(14, t.Recurrence.Times[0].Hour);
                Assert.Equal(0, t.Recurrence.Times[0].Minute);

                var rrule = t.Recurrence.ToRRule();
                Assert.Equal("WEEKLY", rrule["FREQ"]);
                Assert.Equal("MO", rrule["BYDAY"]);
                Assert.Equal("14", rrule["BYHOUR"]);
                Assert.Equal("0", rrule["BYMINUTE"]);
            })
            .AssertPassed();
    }

    [Theory]
    [InlineData("*day/2")]
    [InlineData("*1mon+2p")]
    [InlineData("*lastfri+17:00")]
    [InlineData("*month/3+09:00@2025-01-01~2025-12-31")]
    public Task Parses_Recurrence_Shapes(string recur)
    {
        var line = $"O [r1] {recur} -- Demo";
        var now = new DateTimeOffset(2025, 08, 12, 12, 00, 00, TimeSpan.Zero);
        return Given(() => (line, now))
            .When(t => Parser.ParseLine(t.line, Tz, t.now))
            .Then(tt => Assert.False(tt.Recurrence.IsEmpty))
            .AssertPassed();
    }

    [Theory]
    [InlineData("O work1 -- missing brackets")]
    [InlineData("Z [bad] -- bad status")]
    [InlineData("O [bad id] -- spaces")]
    public Task Rejects_Bad_Inputs(string line)
    {
        var now = DateTimeOffset.UtcNow;
        return Given(() => (line, now))
            .When(t => new Action(() => Parser.ParseLine(t.line, Tz, t.now)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();
    }

    [Fact]
    public Task Priority_Flag_From_Bang_Token()
    {
        return Given(() => "O [a] ! -- title")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Priority);
                Assert.Equal(TaskStatus.Open, t.Status);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Priority_Flag_From_OBang_BackCompat()
    {
        return Given(() => "O! [a] -- title")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Priority);
                Assert.Equal(TaskStatus.Open, t.Status);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Blocked_Computed_From_Dependencies()
    {
        return Given(() => ("X [a] -- done", "O [b] +[a] -- ok", "O [c] +[b] -- blocked"))
            .When(triple => new[]
            {
                Parser.ParseLine(triple.Item1, TestUtil.ChicagoTz, TestUtil.FixedNowUtc),
                Parser.ParseLine(triple.Item2, TestUtil.ChicagoTz, TestUtil.FixedNowUtc),
                Parser.ParseLine(triple.Item3, TestUtil.ChicagoTz, TestUtil.FixedNowUtc)
            })
            .Then(arr =>
            {
                var a = arr[0];
                var b = arr[1];
                var c = arr[2];
                var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
                {
                    ["a"] = a, ["b"] = b, ["c"] = c
                };

                Assert.False(Parser.IsBlocked(b, map)); // a is done
                Assert.True(Parser.IsBlocked(c, map));  // b is open
            })
            .AssertPassed();
    }

    [Fact]
    public Task Blocked_Explicit_Overrides()
    {
        return Given(() => "O [x] ? -- explicit block")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                var map = new Dictionary<string, TodoTask> { ["x"] = t };
                var (blocked, reason) = Parser.ComputeBlockState(t, map);
                Assert.True(blocked);
                Assert.Equal("explicit", reason);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Recurrence_ToString_Canonical_And_Friendly()
    {
        return Given(() => "day/2+8a+8p@2025-01-01~count:5")
            .When(Parser.ParseRecurrence)
            .Then(r =>
            {
                Assert.Equal("day/2+08:00+20:00@2025-01-01~count:5", r.ToString());
                Assert.Equal("day/2+8a+8p@2025-01-01~count:5", r.ToString(friendlyTimes:true));
            })
            .AssertPassed();
    }

    [Fact]
    public Task Recurrence_ToString_Hour_MinuteOnly_Roundtrip()
    {
        return Given(() => "hour/3+15+45")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("hour/3+15+45", r.ToString()))
            .AssertPassed();
    }
}