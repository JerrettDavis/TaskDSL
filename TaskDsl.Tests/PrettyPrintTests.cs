using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class PrettyPrintTests
{
    private readonly ITestOutputHelper _output;
    private static readonly TimeZoneInfo Tz = TestUtil.ChicagoTz;
    private static readonly DateTimeOffset Now = TestUtil.FixedNowUtc;

    public PrettyPrintTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Pretty_Prints_Simple_Open_Task()
    {
        var t = Parser.ParseLine("O [cleanup] ^jd -work -bgis -- Clean up go-live checklist", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Simple Open Task ----");
        _output.WriteLine(pretty);
        _output.WriteLine("----------------------------------------");

        Assert.Contains("[ ]", pretty);       // open icon
        Assert.Contains("(cleanup)", pretty); // id shown
        Assert.Contains("Clean up go-live checklist", pretty);

        // Order-agnostic check for tags
        Assert.Matches(@"Tags:\s*(?:\#work,\s*\#bgis|#bgis,\s*\#work)", pretty);

        Assert.Contains("@jd", pretty);
    }


    [Fact]
    public void Pretty_Prints_Done_Task_With_Checkmark()
    {
        var t = Parser.ParseLine("X [done1] -ops =45m -- Rotate logs", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Done Task ----");
        _output.WriteLine(pretty);

        Assert.Contains("[✓]", pretty);
        Assert.Contains("(done1)", pretty);
        Assert.Contains("Rotate logs", pretty);
        Assert.Contains("Tags: #ops", pretty);
    }

    [Fact]
    public void Pretty_Prints_Priority_And_Explicit_Block_Flags()
    {
        var t = Parser.ParseLine("O [deploy] ! ? +[cleanup] -work *1mon+2p @office -- Deploy to production", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Priority + Blocked ----");
        _output.WriteLine(pretty);

        // Our suggested icons: [✗] overrides blocked, plus ★ for priority
        Assert.Contains("★", pretty);   // priority star
        Assert.Contains("[✗]", pretty); // explicit blocked mark
        Assert.Contains("After: cleanup", pretty);
        Assert.Contains("Repeat: 1mon+2p", pretty); // friendly-times is enabled in example impl
        Assert.Contains("@office", pretty);
    }

    [Fact]
    public void Pretty_Prints_Due_Estimate_Meta_And_Contexts()
    {
        var t = Parser.ParseLine("O [hotfix] ! p:1 =2h >2025-09-01T14:30:00-05:00 -work meta:ticket=BG-42 @office -- Ship hotfix", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Due/Estimate/Meta/Context ----");
        _output.WriteLine(pretty);

        Assert.Contains("Ship hotfix", pretty);
        Assert.Contains("(hotfix)", pretty);
        Assert.Contains("Tags: #work", pretty);
        Assert.Contains("@office", pretty);
        Assert.Contains("Due:", pretty); // exact format depends on your ToPrettyString date rendering
    }

    [Fact]
    public void Pretty_Prints_Quoted_Tokens_And_Escaped_Title()
    {
        var t = Parser.ParseLine("O [fac] -\"support team\" @\"HQ North\" -- Replace filters \\-- lobby", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Quoted Tokens + Escaped Title ----");
        _output.WriteLine(pretty);

        Assert.Contains("Replace filters -- lobby", pretty);
        Assert.Contains("(fac)", pretty);
        Assert.Contains("Tags: #support team", pretty);
        Assert.Contains("@HQ North", pretty);
    }

    [Fact]
    public void Pretty_Prints_Weekday_MultiTimes()
    {
        var t = Parser.ParseLine("O [standup] -team *wed+8a+1p -- Team standup", Tz, Now);
        var pretty = t.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Weekday + Multiple Times ----");
        _output.WriteLine(pretty);

        Assert.Contains("Team standup", pretty);
        Assert.Contains("(standup)", pretty);
        Assert.Contains("Repeat:", pretty);
        Assert.Contains("wed", pretty, StringComparison.OrdinalIgnoreCase);
        // Depending on friendly format you chose: "8a" and "1p" or "08:00" and "13:00"
        Assert.True(pretty.Contains("8a") || pretty.Contains("08:00"));
        Assert.True(pretty.Contains("1p") || pretty.Contains("13:00"));
    }

    [Fact]
    public void Pretty_Prints_Computed_Blocked_When_Dependency_Open()
    {
        var a = Parser.ParseLine("O [build] -ci -- Build step", Tz, Now);
        var b = Parser.ParseLine("O [deploy] +[build] -ci -- Deploy step", Tz, Now);

        var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
            { ["build"] = a, ["deploy"] = b };

        var (blocked, reason) = Parser.ComputeBlockState(b, map);
        var pretty = b.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Computed Blocked ----");
        _output.WriteLine(pretty);
        _output.WriteLine($"ComputedBlocked={blocked} Reason={reason}");

        Assert.True(blocked);
        Assert.Contains("(deploy)", pretty);
        // The pretty printer shows explicit blocked (✗) for BlockedExplicit; for computed blocked
        // you may show nothing or add a suffix like " (blocked)". If you add that, assert it here:
        // Assert.Contains("(blocked)", pretty);
    }

    [Fact]
    public void Pretty_Prints_Not_Blocked_When_Dependency_Done()
    {
        var a = Parser.ParseLine("X [build] -ci -- Build step", Tz, Now);
        var b = Parser.ParseLine("O [deploy] +[build] -ci -- Deploy step", Tz, Now);

        var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
            { ["build"] = a, ["deploy"] = b };

        var (blocked, _) = Parser.ComputeBlockState(b, map);
        var pretty = b.ToPrettyString();

        _output.WriteLine("---- Pretty Print: Not Blocked ----");
        _output.WriteLine(pretty);
        _output.WriteLine($"ComputedBlocked={blocked}");

        Assert.False(blocked);
        Assert.DoesNotContain("[✗]", pretty); // not explicitly blocked
    }
}