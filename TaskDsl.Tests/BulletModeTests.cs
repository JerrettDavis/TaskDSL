namespace TaskDsl.Tests;

public class BulletModeTests
{
    [Fact]
    public void Parses_Open_Bullet_With_Tags_And_Assignees()
    {
        var t = Parser.ParseLine("- buy milk #errand @jd", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal(TaskStatus.Open, t.Status);
        Assert.Equal("buy milk", t.Title);
        Assert.Contains("errand", t.Tags);
        Assert.Contains("jd", t.Assignees);
        Assert.Matches(@"^[a-z0-9-]+-[a-f0-9]{6}$", t.Id);
    }

    [Fact]
    public void Parses_Done_Bullet_With_Dependency()
    {
        var t = Parser.ParseLine("~~ set DNS record #infra +[ticket123]", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal(TaskStatus.Done, t.Status);
        Assert.Contains("infra", t.Tags);
        Assert.Contains("ticket123", t.Dependencies);
        Assert.Equal("set DNS record", t.Title);
    }

    [Fact]
    public void Bullet_Roundtrips_Via_Canonical_Dsl()
    {
        var a = Parser.ParseLine("- swap air filter #home @sam", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var canonical = a.ToString(); // becomes full DSL with [id]
        var b = Parser.ParseLine(canonical, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);

        Assert.Equal(a.Title, b.Title);
        Assert.True(a.Tags.SetEquals(b.Tags));
        Assert.True(a.Assignees.SetEquals(b.Assignees));
        Assert.Equal(a.Status, b.Status);
    }

    [Fact]
    public void ToBulletString_Preserves_Simple_Shapes()
    {
        var a = Parser.ParseLine("- swap air filter #home @sam +[hvac-ticket]", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        var bullet = a.ToBulletString();

        // Should look like a bullet again
        Assert.StartsWith("-", bullet);
        Assert.Contains("#home", bullet);
        Assert.Contains("@sam", bullet);
        Assert.Contains("+[hvac-ticket]", bullet);
    }

    [Fact]
    public void ToBulletString_Falls_Back_When_Not_Simple()
    {
        var a = Parser.ParseLine("- take meds #health", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        a.Recurrence = Parser.ParseRecurrence("day/2+8a+8p"); // now not simple

        var bulletish = a.ToBulletString();
        Assert.StartsWith("O [", bulletish); // fell back to canonical DSL
        Assert.Contains("*day/2", bulletish);
    }
}