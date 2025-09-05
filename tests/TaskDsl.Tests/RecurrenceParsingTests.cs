using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class RecurrenceParsingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Theory]
    [InlineData("*day")]
    [InlineData("*day/2")]
    [InlineData("*hour/3")]
    [InlineData("*week/2")]
    [InlineData("*month")]
    [InlineData("*year/5")]
    public Task Basic_Frequencies_Accepted(string recur)
        => Given(() => recur)
            .When(r => Parser.ParseLine($"O [r] {r} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t => Assert.False(t.Recurrence.IsEmpty))
            .AssertPassed();

    [Theory]
    [InlineData("*mon")]
    [InlineData("*tue")]
    [InlineData("*wed")]
    [InlineData("*thu")]
    [InlineData("*fri")]
    [InlineData("*sat")]
    [InlineData("*sun")]
    public Task Weekday_Frequencies_Accepted(string recur)
        => Given(() => recur)
            .When(r => Parser.ParseLine($"O [r] {r}+9a -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.False(t.Recurrence.IsEmpty);
                Assert.Single(t.Recurrence.Times);
                Assert.Equal(9, t.Recurrence.Times[0].Hour);
            })
            .AssertPassed();

    [Theory]
    [InlineData("*1mon")]
    [InlineData("*2tue")]
    [InlineData("*3wed")]
    [InlineData("*4thu")]
    [InlineData("*5fri")]
    [InlineData("*lastsat")]
    [InlineData("*lastsun")]
    public Task Nth_And_Last_Weekday_In_Month_Accepted(string recur)
        => Given(() => recur)
            .When(r => Parser.ParseLine($"O [r] {r}+14:30 -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.False(t.Recurrence.IsEmpty);
                Assert.Equal(14, t.Recurrence.Times[0].Hour);
                Assert.Equal(30, t.Recurrence.Times[0].Minute);
            })
            .AssertPassed();

    [Fact]
    public Task Multiple_Times_Accepted()
        => Given(() => "O [r] *wed+8a+1p+22:05 -- x")
            .When(line => Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.Equal(new[] { 8, 13, 22 }, t.Recurrence.Times.Select(x => x.Hour).ToArray());
                Assert.Equal(new[] { 0, 0, 5 }, t.Recurrence.Times.Select(x => x.Minute).ToArray());
            })
            .AssertPassed();

    [Fact]
    public Task Start_End_And_Count_Accepted()
        => Given(() => "O [r] *month/3@2025-01-01~2025-12-31 -- x")
            .When(line => Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t1 =>
            {
                Assert.Equal("month", t1.Recurrence.Freq);
                Assert.Equal(3, t1.Recurrence.Interval);
                Assert.Equal(new(2025, 1, 1), t1.Recurrence.Start);
                Assert.Equal(new(2025, 12, 31), t1.Recurrence.End);
            })
            .And(_ =>
            {
                // second case
                var t2 = Parser.ParseLine("O [r] *day~count:10 -- y", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
                Assert.Equal(10, t2.Recurrence.Count);
            })
            .AssertPassed();

    [Theory]
    [InlineData("*foo")]
    [InlineData("*8xyz")]
    [InlineData("*1friday")] // must be 1fri
    [InlineData("*mon/0")]   // interval must be >=1 (currently parser allows any int; tighten if desired)
    public Task Bad_Recurrence_Freq_Throws(string recur)
        => Given(() => recur)
            .When(r => new Action(() => Parser.ParseLine($"O [r] {r} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();
}