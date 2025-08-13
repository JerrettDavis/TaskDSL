namespace TaskDsl.Tests;

public class RecurrenceParsingTests
{
    [Theory]
    [InlineData("*day")]
    [InlineData("*day/2")]
    [InlineData("*hour/3")]
    [InlineData("*week/2")]
    [InlineData("*month")]
    [InlineData("*year/5")]
    public void Basic_Frequencies_Accepted(string recur)
    {
        var t = Parser.ParseLine($"O [r] {recur} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.False(t.Recurrence.IsEmpty);
    }

    [Theory]
    [InlineData("*mon")]
    [InlineData("*tue")]
    [InlineData("*wed")]
    [InlineData("*thu")]
    [InlineData("*fri")]
    [InlineData("*sat")]
    [InlineData("*sun")]
    public void Weekday_Frequencies_Accepted(string recur)
    {
        var t = Parser.ParseLine($"O [r] {recur}+9a -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.False(t.Recurrence.IsEmpty);
        Assert.Single(t.Recurrence.Times);
        Assert.Equal(9, t.Recurrence.Times[0].Hour);
    }

    [Theory]
    [InlineData("*1mon")]
    [InlineData("*2tue")]
    [InlineData("*3wed")]
    [InlineData("*4thu")]
    [InlineData("*5fri")]
    [InlineData("*lastsat")]
    [InlineData("*lastsun")]
    public void Nth_And_Last_Weekday_In_Month_Accepted(string recur)
    {
        var t = Parser.ParseLine($"O [r] {recur}+14:30 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.False(t.Recurrence.IsEmpty);
        Assert.Equal(14, t.Recurrence.Times[0].Hour);
        Assert.Equal(30, t.Recurrence.Times[0].Minute);
    }

    [Fact]
    public void Multiple_Times_Accepted()
    {
        var t = Parser.ParseLine("O [r] *wed+8a+1p+22:05 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal(new[] { 8, 13, 22 }, t.Recurrence.Times.Select(x => x.Hour).ToArray());
        Assert.Equal(new[] { 0, 0, 5 }, t.Recurrence.Times.Select(x => x.Minute).ToArray());
    }

    [Fact]
    public void Start_End_And_Count_Accepted()
    {
        var t1 = Parser.ParseLine("O [r] *month/3@2025-01-01~2025-12-31 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal("month", t1.Recurrence.Freq);
        Assert.Equal(3, t1.Recurrence.Interval);
        Assert.Equal(new(2025, 1, 1), t1.Recurrence.Start);
        Assert.Equal(new(2025, 12, 31), t1.Recurrence.End);
        var t2 = Parser.ParseLine("O [r] *day~count:10 -- y", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal(10, t2.Recurrence.Count);
    }

    [Theory]
    [InlineData("*foo")]
    [InlineData("*8xyz")]
    [InlineData("*1friday")] // must be 1fri
    [InlineData("*mon/0")]   // interval must be >=1 (currently parser allows any int; tighten if desired)
    public void Bad_Recurrence_Freq_Throws(string recur)
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine($"O [r] {recur} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }
}