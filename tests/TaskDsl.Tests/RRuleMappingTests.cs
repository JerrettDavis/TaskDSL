using TaskDsl.Extensions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class RRuleMappingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Weekday_Maps_To_Weekly_ByDay()
        => Given(() => "O [r] *mon+2p -- x")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                var r = t.Recurrence.ToRRule();
                Assert.Equal("WEEKLY", r["FREQ"]);
                Assert.Equal("MO", r["BYDAY"]);
                Assert.Equal("14", r["BYHOUR"]);
                Assert.Equal("0", r["BYMINUTE"]);
            })
            .AssertPassed();

    [Fact]
    public Task NthWeekday_Maps_To_Monthly_BySetPos()
        => Given(() => "O [r] *1mon+09:00 -- x")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                var r = t.Recurrence.ToRRule();
                Assert.Equal("MONTHLY", r["FREQ"]);
                Assert.Equal("MO", r["BYDAY"]);
                Assert.Equal("1", r["BYSETPOS"]);
                Assert.Equal("9", r["BYHOUR"]);
                Assert.Equal("0", r["BYMINUTE"]);
            })
            .AssertPassed();

    [Fact]
    public Task LastWeekday_Maps_To_Negative_SetPos()
        => Given(() => "O [r] *lastfri+17:00 -- x")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                var r = t.Recurrence.ToRRule();
                Assert.Equal("MONTHLY", r["FREQ"]);
                Assert.Equal("FR", r["BYDAY"]);
                Assert.Equal("-1", r["BYSETPOS"]);
                Assert.Equal("17", r["BYHOUR"]);
                Assert.Equal("0", r["BYMINUTE"]);
            })
            .AssertPassed();

    [Fact]
    public Task Interval_Count_Until_Are_Emitted()
        => Given(() => "O [r] *day/2@2025-01-01~2025-12-31 -- x")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                var r = t.Recurrence.ToRRule();
                Assert.Equal("DAILY", r["FREQ"]);
                Assert.Equal("2", r["INTERVAL"]);
                Assert.Equal("20251231T235959Z", r["UNTIL"]);
            })
            .AssertPassed();
}