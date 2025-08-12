namespace TaskDsl.Tests;

public class RRuleMappingTests
{
    [Fact]
    public void Weekday_Maps_To_Weekly_ByDay()
    {
        var t = Parser.ParseLine("O [r] *mon+2p -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var r = Parser.ToRRule(t.Recurrence);
        Assert.Equal("WEEKLY", r["FREQ"]);
        Assert.Equal("MO", r["BYDAY"]);
        Assert.Equal("14", r["BYHOUR"]);
        Assert.Equal("0", r["BYMINUTE"]);
    }

    [Fact]
    public void NthWeekday_Maps_To_Monthly_BySetPos()
    {
        var t = Parser.ParseLine("O [r] *1mon+09:00 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var r = Parser.ToRRule(t.Recurrence);
        Assert.Equal("MONTHLY", r["FREQ"]);
        Assert.Equal("MO", r["BYDAY"]);
        Assert.Equal("1", r["BYSETPOS"]);
        Assert.Equal("9", r["BYHOUR"]);
        Assert.Equal("0", r["BYMINUTE"]);
    }

    [Fact]
    public void LastWeekday_Maps_To_Negative_SetPos()
    {
        var t = Parser.ParseLine("O [r] *lastfri+17:00 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var r = Parser.ToRRule(t.Recurrence);
        Assert.Equal("MONTHLY", r["FREQ"]);
        Assert.Equal("FR", r["BYDAY"]);
        Assert.Equal("-1", r["BYSETPOS"]);
        Assert.Equal("17", r["BYHOUR"]);
        Assert.Equal("0", r["BYMINUTE"]);
    }

    [Fact]
    public void Interval_Count_Until_Are_Emitted()
    {
        var t = Parser.ParseLine("O [r] *day/2@2025-01-01~2025-12-31 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var r = Parser.ToRRule(t.Recurrence);
        Assert.Equal("DAILY", r["FREQ"]);
        Assert.Equal("2", r["INTERVAL"]);
        Assert.Equal("20251231T235959Z", r["UNTIL"]);
    }
}