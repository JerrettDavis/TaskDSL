using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class DueParsingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Absolute_DateTime_Is_Parsed()
        => Given(() => "O [t] >2025-09-01T14:30:00-05:00 -- x")
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Due.HasValue);
                Assert.Equal(2025, t.Due!.Value.Year);
                Assert.Equal(9, t.Due!.Value.Month);
                Assert.Equal(1, t.Due!.Value.Day);
            })
            .AssertPassed();

    [Fact]
    public Task Weekday_With_Time_Defaults_To_Next_That_Day()
        => Given(() => "O [t] >fri+5p -- x")
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Due.HasValue);
                Assert.True(t.Due!.Value > TestUtil.FixedNowUtc);
            })
            .AssertPassed();

    [Theory]
    [InlineData("O [t] >sun+5p -- x")]
    [InlineData("O [t] >sat+5p -- x")]
    [InlineData("O [t] >mon+5p -- x")]
    [InlineData("O [t] >tue+5p -- x")]
    [InlineData("O [t] >wed+5p -- x")]
    [InlineData("O [t] >thu+5p -- x")]
    public Task Weekday_With_Time_Always_Uses_Next_Occurrence(string line)
        => Given(() => line)
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Due.HasValue);
                Assert.True(t.Due!.Value > TestUtil.FixedNowUtc);
            })
            .AssertPassed();

    [Theory]
    [InlineData(">8a")]
    [InlineData(">14:30")]
    [InlineData(">2:05p")]
    public Task TimeOnly_Sets_Today_Or_Next_Day(string due)
        => Given(() => due)
            .When(d => Parser.ParseLine($"O [t] {d} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Due.HasValue);
                Assert.True(t.Due!.Value > TestUtil.FixedNowUtc);
            })
            .AssertPassed();

    [Fact]
    public Task Weekday_Without_Time_Defaults_To_FivePm()
        => Given(() => "O [t] >fri -- x")
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.True(t.Due.HasValue);
                var localDue = TimeZoneInfo.ConvertTime(t.Due!.Value, TestUtil.ChicagoTz);
                Assert.Equal(17, localDue.Hour);
                Assert.Equal(0, localDue.Minute);
            })
            .AssertPassed();
}