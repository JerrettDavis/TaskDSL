using Xunit.Abstractions;
using TinyBDD.Xunit;

namespace TaskDsl.Tests;

public class PrettyPrintTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private readonly ITestOutputHelper _output = output;
    private static readonly TimeZoneInfo Tz = TestUtil.ChicagoTz;
    private static readonly DateTimeOffset Now = TestUtil.FixedNowUtc;

    [Fact]
    public Task Pretty_Prints_Simple_Open_Task()
    {
        return Given(() => "O [cleanup] ^jd -work -bgis -- Clean up go-live checklist")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
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
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Done_Task_With_Checkmark()
    {
        return Given(() => "X [done1] -ops =45m -- Rotate logs")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
                var pretty = t.ToPrettyString();
                _output.WriteLine(pretty);

                Assert.Contains("[✓]", pretty);
                Assert.Contains("(done1)", pretty);
                Assert.Contains("Rotate logs", pretty);
                Assert.Contains("Tags: #ops", pretty);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Priority_And_Explicit_Block_Flags()
    {
        return Given(() => "O [deploy] ! ? +[cleanup] -work *1mon+2p @office -- Deploy to production")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
                var pretty = t.ToPrettyString();
                _output.WriteLine(pretty);

                Assert.Contains("★", pretty);   // priority star
                Assert.Contains("[✗]", pretty); // explicit blocked mark
                Assert.Contains("After: cleanup", pretty);
                Assert.Contains("Repeat:", pretty);
                Assert.Contains("@office", pretty);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Due_Estimate_Meta_And_Contexts()
    {
        return Given(() => "O [hotfix] ! p:1 =2h >2025-09-01T14:30:00-05:00 -work meta:ticket=BG-42 @office -- Ship hotfix")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
                var pretty = t.ToPrettyString();
                _output.WriteLine(pretty);

                Assert.Contains("Ship hotfix", pretty);
                Assert.Contains("(hotfix)", pretty);
                Assert.Contains("Tags: #work", pretty);
                Assert.Contains("@office", pretty);
                Assert.Contains("Due:", pretty);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Quoted_Tokens_And_Escaped_Title()
    {
        return Given(() => "O [fac] -\"support team\" @\"HQ North\" -- Replace filters \\-- lobby")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
                var pretty = t.ToPrettyString();
                _output.WriteLine(pretty);

                Assert.Contains("Replace filters -- lobby", pretty);
                Assert.Contains("(fac)", pretty);
                Assert.Contains("Tags: #support team", pretty);
                Assert.Contains("@HQ North", pretty);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Weekday_MultiTimes()
    {
        return Given(() => "O [standup] -team *wed+8a+1p -- Team standup")
            .When(l => Parser.ParseLine(l, Tz, Now))
            .Then(t =>
            {
                var pretty = t.ToPrettyString();
                _output.WriteLine(pretty);

                Assert.Contains("Team standup", pretty);
                Assert.Contains("(standup)", pretty);
                Assert.Contains("Repeat:", pretty);
                Assert.Contains("wed", pretty, StringComparison.OrdinalIgnoreCase);
                Assert.True(pretty.Contains("8a") || pretty.Contains("08:00"));
                Assert.True(pretty.Contains("1p") || pretty.Contains("13:00"));
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Computed_Blocked_When_Dependency_Open()
    {
        return Given(() => ("O [build] -ci -- Build step", "O [deploy] +[build] -ci -- Deploy step"))
            .When(pair => new[] { Parser.ParseLine(pair.Item1, Tz, Now), Parser.ParseLine(pair.Item2, Tz, Now) })
            .Then(arr =>
            {
                var a = arr[0];
                var b = arr[1];
                var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
                { ["build"] = a, ["deploy"] = b };

                var (blocked, reason) = Parser.ComputeBlockState(b, map);
                var pretty = b.ToPrettyString();
                _output.WriteLine(pretty);
                _output.WriteLine($"ComputedBlocked={blocked} Reason={reason}");

                Assert.True(blocked);
                Assert.Contains("(deploy)", pretty);
            })
            .AssertPassed();
    }

    [Fact]
    public Task Pretty_Prints_Not_Blocked_When_Dependency_Done()
    {
        return Given(() => ("X [build] -ci -- Build step", "O [deploy] +[build] -ci -- Deploy step"))
            .When(pair => new[] { Parser.ParseLine(pair.Item1, Tz, Now), Parser.ParseLine(pair.Item2, Tz, Now) })
            .Then(arr =>
            {
                var a = arr[0];
                var b = arr[1];
                var map = new Dictionary<string, TodoTask>(StringComparer.OrdinalIgnoreCase)
                { ["build"] = a, ["deploy"] = b };

                var (blocked, _) = Parser.ComputeBlockState(b, map);
                var pretty = b.ToPrettyString();
                _output.WriteLine(pretty);
                _output.WriteLine($"ComputedBlocked={blocked}");

                Assert.False(blocked);
                Assert.DoesNotContain("[✗]", pretty); // not explicitly blocked
            })
            .AssertPassed();
    }
}