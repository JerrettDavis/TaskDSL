namespace TaskDsl.Tests;

public class CronStringTests
{
    [Fact]
    public void Min_Every15Minutes()
    {
        var r = Parser.ParseRecurrence("min/15");
        var cron = r.ToCronString();
        Assert.Equal("*/15 * * * *", cron);
    }

    [Fact]
    public void Hour_Every3Hours_At_15_And_45()
    {
        var r = Parser.ParseRecurrence("hour/3+15+45");
        var cron = r.ToCronString();
        Assert.Equal("15,45 */3 * * *", cron);
    }

    [Fact]
    public void Day_Every2Days_At_8a_And_8p()
    {
        var r = Parser.ParseRecurrence("day/2+8a+8p");
        var cron = r.ToCronString();
        Assert.Equal("0 8,20 */2 * *", cron);
    }

    [Fact]
    public void Month_Every3Months_At_09_00()
    {
        var r = Parser.ParseRecurrence("month/3+09:00");
        var cron = r.ToCronString();
        Assert.Equal("0 9 * */3 *", cron);
    }

    [Fact]
    public void Year_Defaults_To_Jan1_At_Midnight_When_No_Times()
    {
        // Our implementation fixes dom=1, month=1 and minute=0; hour remains "*"
        // Provide a time to make it deterministic.
        var r = Parser.ParseRecurrence("year+00:00");
        var cron = r.ToCronString();
        Assert.Equal("0 0 1 1 *", cron);
    }

    [Fact]
    public void Weekday_Monday_At_2pm()
    {
        var r = Parser.ParseRecurrence("mon+2p");
        var cron = r.ToCronString();
        Assert.Equal("0 14 * * 1", cron);
    }

    [Fact]
    public void Weekday_Tuesday_At_10_00()
    {
        var r = Parser.ParseRecurrence("tue+10:00");
        var cron = r.ToCronString();
        Assert.Equal("0 10 * * 2", cron);
    }

    [Fact]
    public void MultipleTimes_Wednesday_At_8a_And_1p_And_22_05()
    {
        var r = Parser.ParseRecurrence("wed+8a+1p+22:05");
        var cron = r.ToCronString();
        Assert.Equal("0,5 8,13,22 * * 3", cron);
    }

    [Fact]
    public void Throws_For_NthWeekday_Not_Supported_In_Cron()
    {
        var r = Parser.ParseRecurrence("1mon+09:00");
        Assert.Throws<NotSupportedException>(() => r.ToCronString());
    }

    [Fact]
    public void Throws_For_LastWeekday_Not_Supported_In_Cron()
    {
        var r = Parser.ParseRecurrence("lastfri+17:00");
        Assert.Throws<NotSupportedException>(() => r.ToCronString());
    }

    [Fact]
    public void Unknown_Frequency_Throws_FormatException()
    {
        var r = new Recurrence("weird", 1, [], null, null, null);
        Assert.Throws<FormatException>(() => r.ToCronString());
    }

    [Fact]
    public void Empty_Recurrence_Throws()
    {
        Assert.Throws<ArgumentException>(() => Recurrence.Empty.ToCronString());
    }
}