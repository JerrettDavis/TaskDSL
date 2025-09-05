using TaskDsl.Extensions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class CronStringTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Min_Every15Minutes()
        => Given(() => "min/15")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("*/15 * * * *", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Hour_Every3Hours_At_15_And_45()
        => Given(() => "hour/3+15+45")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("15,45 */3 * * *", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Day_Every2Days_At_8a_And_8p()
        => Given(() => "day/2+8a+8p")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0 8,20 */2 * *", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Month_Every3Months_At_09_00()
        => Given(() => "month/3+09:00")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0 9 * */3 *", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Year_Defaults_To_Jan1_At_Midnight_When_No_Times()
        => Given(() => "year+00:00")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0 0 1 1 *", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Weekday_Monday_At_2pm()
        => Given(() => "mon+2p")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0 14 * * 1", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Weekday_Tuesday_At_10_00()
        => Given(() => "tue+10:00")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0 10 * * 2", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task MultipleTimes_Wednesday_At_8a_And_1p_And_22_05()
        => Given(() => "wed+8a+1p+22:05")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Equal("0,5 8,13,22 * * 3", r.ToCronString()))
            .AssertPassed();

    [Fact]
    public Task Throws_For_NthWeekday_Not_Supported_In_Cron()
        => Given(() => "1mon+09:00")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Throws<NotSupportedException>(r.ToCronString))
            .AssertPassed();

    [Fact]
    public Task Throws_For_LastWeekday_Not_Supported_In_Cron()
        => Given(() => "lastfri+17:00")
            .When(Parser.ParseRecurrence)
            .Then(r => Assert.Throws<NotSupportedException>(r.ToCronString))
            .AssertPassed();

    [Fact]
    public Task Unknown_Frequency_Throws_FormatException()
        => Given(() => new Recurrence("weird", 1, [], null, null, null))
            .When(r => new Action(() => r.ToCronString()))
            .Then(a => Assert.Throws<FormatException>(a))
            .AssertPassed();

    [Fact]
    public Task Empty_Recurrence_Throws()
        => Given(() => new Action(() => Recurrence.Empty.ToCronString()))
            .Then(a => Assert.Throws<ArgumentException>(a))
            .AssertPassed();
}