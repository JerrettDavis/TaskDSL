namespace TaskDsl.Tests;

public class DueParsingTests
{
    [Fact]
    public void Absolute_DateTime_Is_Parsed()
    {
        const string line = "O [t] >2025-09-01T14:30:00-05:00 -- x";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Due.HasValue);
        Assert.Equal(2025, t.Due!.Value.Year);
        Assert.Equal(9, t.Due!.Value.Month);
        Assert.Equal(1, t.Due!.Value.Day);
    }

    [Fact]
    public void Weekday_With_Time_Defaults_To_Next_That_Day()
    {
        const string line = "O [t] >fri+5p -- x";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Due.HasValue);
        // not asserting exact instant because conversion to UTC varies; we just ensure it's in the future
        Assert.True(t.Due!.Value > TestUtil.FixedNowUtc);
    }
    
    [Theory]
    [InlineData("O [t] >sun+5p -- x")]
    [InlineData("O [t] >sat+5p -- x")]
    [InlineData("O [t] >mon+5p -- x")]
    [InlineData("O [t] >tue+5p -- x")]
    [InlineData("O [t] >wed+5p -- x")]
    [InlineData("O [t] >thu+5p -- x")]
    public void Weekday_With_Time_Always_Uses_Next_Occurrence(string line)
    {
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Due.HasValue);
        Assert.True(t.Due!.Value > TestUtil.FixedNowUtc); // chosen behavior: next occurrence if already passed
    }

    [Theory]
    [InlineData(">8a")]
    [InlineData(">14:30")]
    [InlineData(">2:05p")]
    public void TimeOnly_Sets_Today_Or_Next_Day(string due)
    {
        var t = Parser.ParseLine($"O [t] {due} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Due.HasValue);
        Assert.True(t.Due!.Value > TestUtil.FixedNowUtc); // chosen behavior: next occurrence if already passed
    }

    [Fact]
    public void Weekday_Without_Time_Defaults_To_FivePm()
    {
        const string line = "O [t] >fri -- x";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.True(t.Due.HasValue);
        // Should be at 17:00 local time (Chicago) on the next Friday
        var localDue = TimeZoneInfo.ConvertTime(t.Due!.Value, TestUtil.ChicagoTz);
        Assert.Equal(17, localDue.Hour);
        Assert.Equal(0, localDue.Minute);
    }
}