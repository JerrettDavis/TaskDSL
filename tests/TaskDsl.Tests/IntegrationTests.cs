using TaskDsl.Extensions;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class IntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Parses_Realistic_Todo_File_EndToEnd()
        => Given("fixed now and chicago tz", () => (TestUtil.FixedNowUtc, TestUtil.ChicagoTz))
            .When("build tasks from sample lines", ctx => BuildTasks(ctx.Item1, ctx.Item2))
            .Then("validate parsed tasks", tasks => AssertIntegrationTasks(tasks, TestUtil.FixedNowUtc))
            .AssertPassed();

    // Helper that builds the list of tasks from sample lines
    private static List<TodoTask> BuildTasks(DateTimeOffset now, TimeZoneInfo tz)
    {
        var lines = new[]
        {
            // 1) Simple root item
            "O [root] -inbox -- Capture random ideas",

            // 3) Weekday due with time + quoted assignee and contexts
            "O [meet1] ^\"sam j\" @office >fri+5p -meeting -- 1:1 with Sam",

            // 4) Daily twice at 8a and 8p
            "O [water] -health *day/2+8a+8p -- Drink water",

            // 5) Basic work task with tags
            "O [cleanup] -work -bgis -- Clean up go-live checklist",

            // 6) Dependent on cleanup + monthly first Monday at 2p
            "O [deploy] +[cleanup] -work *1mon+2p -- Deploy to production",

            // 8) Completed item with estimate and tags
            "X [done1] -ops =45m -- Rotate logs",

            // 9) Last Friday monthly, 5pm
            "O [finance-report] -finance *lastfri+17:00 -- Run monthly finance report",

            // 10) Weekly Wednesday at two times
            "O [standup] -team *wed+9a+1p -- Team standup sessions",

            // 11) Quarterly schedule with start/end window
            "O [quarterly-plan] -planning *month/3@2025-01-01~2025-12-31 -- Quarterly planning",

            // 12) Time-only due today or tomorrow depending on now
            "O [today-reminder] >14:30 -reminders -- Ping vendor",

            // 13) Quoted tag/context; title includes escaped double dash
            "O [facilities] -\"support team\" @\"HQ North\" -- Replace air filters \\-- lobby and east wing",

            // 14) Heavier combo: multi-assign, deps, contexts, priority, estimate, meta, recurrence
            "O [release] ^jd ^sam -work -release +[deploy] @office @remote p:2 =90m *tue+10:00 meta:owner=platform -- Release comms"
        };

        return lines.Select(l => Parser.ParseLine(l, tz, now)).ToList();
    }

    // Helper that contains all assertions previously inlined in the Then block
    private static ValueTask AssertIntegrationTasks(List<TodoTask> tasks, DateTimeOffset now)
    {
        // --------- High-level sanity ----------
        Assert.Equal(12, tasks.Count);

        // IDs are unique
        var ids = tasks.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Some category counts
        Assert.Equal(1, tasks.Count(t => t.Status == TaskStatus.Done));
        Assert.Contains(tasks, t => t.Recurrence is { IsEmpty: false });

        // --------- Specific assertions by id ----------
        TodoTask Get(string id) => tasks.Single(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));


        // [meet1] quoted assignee + weekday+time due
        {
            var t = Get("meet1");
            Assert.Contains("sam j", t.Assignees);
            Assert.Contains("office", t.Contexts);
            Assert.True(t.Due!.Value > now);
        }

        // [water] daily twice with explicit times
        {
            var t = Get("water");
            Assert.Equal("day", t.Recurrence.Freq);
            Assert.Equal(2, t.Recurrence.Interval);
            Assert.Equal(new[] { 8, 20 }, t.Recurrence.Times.Select(x => x.Hour).ToArray());
        }

        // [deploy] depends on [cleanup], monthly 1st Monday 2pm, RRULE mapping
        {
            var t = Get("deploy");
            Assert.Contains("cleanup", t.Dependencies);
            Assert.Equal("1mon", t.Recurrence.Freq);
            var rr = t.Recurrence.ToRRule();
            Assert.Equal("MONTHLY", rr["FREQ"]);
            Assert.Equal("MO", rr["BYDAY"]);
            Assert.Equal("1", rr["BYSETPOS"]);
            Assert.Equal("14", rr["BYHOUR"]);
            Assert.Equal("0", rr["BYMINUTE"]);
        }

        // [done1] completed with estimate
        {
            var t = Get("done1");
            Assert.Equal(TaskStatus.Done, t.Status);
            Assert.Equal(TimeSpan.FromMinutes(45), t.Estimate);
            Assert.Contains("ops", t.Tags);
        }

        // [finance-report] last Friday monthly, RRULE
        {
            var t = Get("finance-report");
            var rr = t.Recurrence.ToRRule();
            Assert.Equal("MONTHLY", rr["FREQ"]);
            Assert.Equal("FR", rr["BYDAY"]);
            Assert.Equal("-1", rr["BYSETPOS"]);
            Assert.Equal("17", rr["BYHOUR"]);
            Assert.Equal("0", rr["BYMINUTE"]);
        }

        // [standup] weekly Wed at two times
        {
            var t = Get("standup");
            Assert.Equal("wed", t.Recurrence.Freq);
            Assert.Equal(new[] { 9, 13 }, t.Recurrence.Times.Select(x => x.Hour).ToArray());
        }

        // [quarterly-plan] month/3 with range
        {
            var t = Get("quarterly-plan");
            Assert.Equal("month", t.Recurrence.Freq);
            Assert.Equal(3, t.Recurrence.Interval);
            Assert.Equal(new(2025, 1, 1), t.Recurrence.Start);
            Assert.Equal(new(2025, 12, 31), t.Recurrence.End);
        }

        // [today-reminder] time-only due: ensure in the future relative to now
        {
            var t = Get("today-reminder");
            Assert.True(t.Due!.Value > now);
        }

        // [facilities] quoted tag/context + escaped -- in title
        {
            var t = Get("facilities");
            Assert.Contains("support team", t.Tags);
            Assert.Contains("HQ North", t.Contexts);
            Assert.Equal("Replace air filters -- lobby and east wing", t.Title);
        }

        // [release] heavy combo
        {
            var t = Get("release");
            Assert.True(t.Assignees.SetEquals(["jd", "sam"]));
            Assert.Contains("deploy", t.Dependencies);
            Assert.True(t.Contexts.SetEquals(["office", "remote"]));
            Assert.Equal(TimeSpan.FromMinutes(90), t.Estimate);
            Assert.Equal("platform", t.Meta["owner"]);
            Assert.Equal("tue", t.Recurrence.Freq);
            Assert.Single(t.Recurrence.Times);
            Assert.Equal(10, t.Recurrence.Times[0].Hour);

            var rr = t.Recurrence.ToRRule();
            Assert.Equal("WEEKLY", rr["FREQ"]);
            Assert.Equal("TU", rr["BYDAY"]);
            Assert.Equal("10", rr["BYHOUR"]);
        }

        // --------- Cross-cutting derived checks ----------

        // All dependencies refer to existing IDs
        var idSet = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        foreach (var dep in tasks.SelectMany(t => t.Dependencies))
        {
            Assert.Contains(dep, idSet);
        }

        // At least one task per key category
        Assert.Contains(tasks, t => t.Tags.Contains("work"));
        Assert.Contains(tasks, t => t.Tags.Contains("finance"));
        Assert.Contains(tasks, t => t.Tags.Contains("health"));
        Assert.Contains(tasks, t => t.Tags.Contains("team"));

        // Ensure quoted sigils were consumed as single tokens
        var meet = Get("meet1");
        Assert.Single(meet.Assignees);
        var fac = Get("facilities");
        Assert.Equal(1, fac.Tags.Count(tag => tag == "support team"));
        Assert.Equal(1, fac.Contexts.Count(ctx => ctx == "HQ North"));
        
        return default;
    }
}