using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class BulletModeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Parses_Open_Bullet_With_Tags_And_Assignees()
        => Given(() => "- buy milk #errand @jd")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.Equal(TaskStatus.Open, t.Status);
                Assert.Equal("buy milk", t.Title);
                Assert.Contains("errand", t.Tags);
                Assert.Contains("jd", t.Assignees);
                Assert.Matches(@"^[a-z0-9-]+-[a-f0-9]{6}$", t.Id);
            })
            .AssertPassed();

    [Fact]
    public Task Parses_Done_Bullet_With_Dependency()
        => Given(() => "~~ set DNS record #infra +[ticket123]")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.Equal(TaskStatus.Done, t.Status);
                Assert.Contains("infra", t.Tags);
                Assert.Contains("ticket123", t.Dependencies);
                Assert.Equal("set DNS record", t.Title);
            })
            .AssertPassed();

    [Fact]
    public Task Bullet_Roundtrips_Via_Canonical_Dsl()
        => Given(() => "- swap air filter #home @sam")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(a =>
            {
                var canonical = a.ToString(); // becomes full DSL with [id]
                var b = Parser.ParseLine(canonical, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);

                Assert.Equal(a.Title, b.Title);
                Assert.True(a.Tags.SetEquals(b.Tags));
                Assert.True(a.Assignees.SetEquals(b.Assignees));
                Assert.Equal(a.Status, b.Status);
            })
            .AssertPassed();

    [Fact]
    public Task ToBulletString_Preserves_Simple_Shapes()
        => Given(() => "- swap air filter #home @sam +[hvac-ticket]")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(a =>
            {
                var bullet = a.ToBulletString();

                Assert.StartsWith("-", bullet);
                Assert.Contains("#home", bullet);
                Assert.Contains("@sam", bullet);
                Assert.Contains("+[hvac-ticket]", bullet);
            })
            .AssertPassed();

    [Fact]
    public Task ToBulletString_Falls_Back_When_Not_Simple()
        => Given(() => "- take meds #health")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(a =>
            {
                a.Recurrence = Parser.ParseRecurrence("day/2+8a+8p"); // now not simple

                var bulletish = a.ToBulletString();
                Assert.StartsWith("O [", bulletish); // fell back to canonical DSL
                Assert.Contains("*day/2", bulletish);
            })
            .AssertPassed();
}