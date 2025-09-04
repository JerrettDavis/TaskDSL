using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;


using static ShouldExtensions;

public class AttributeParsingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{

    [Fact]
    public Task Parses_Assignees_Tags_Deps()
    {
        const string line = "O [t] ^jd ^sam -work -bgis +[a] +[b] -- Title";

        return Given(() => line)
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(p => ShouldBe(p.Assignees.SetEquals(["jd", "sam"])))
            .And(p => ShouldBe(p.Tags.SetEquals(["work", "bgis"])))
            .And(p => ShouldEqual(p.Dependencies.ToArray(), ["a", "b"]))
            .And(p => ShouldEqual("Title", p.Title))
            .AssertPassed();
    }

    [Fact]
    public Task Parses_Estimate_Priority_Context_Meta()
    {
        const string line = "O [t] =45m p:3 @home @office meta:source=ops meta:ticket=BG-12 -- Do it";

        return Given(() => line)
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.Equal(TimeSpan.FromMinutes(45), t.Estimate);
                Assert.True(t.Contexts.SetEquals(["home", "office"]));
                Assert.Equal("ops", t.Meta["source"]);
                Assert.Equal("BG-12", t.Meta["ticket"]);
            })
            .AssertPassed();
    }

    [Theory]
    [InlineData("meta:x")]
    [InlineData("meta:=y")]
    [InlineData("meta:x=")]
    public Task Bad_Meta_Throws(string meta)
        => Given(() => meta)
            // Return an Action so the exception is thrown when the action is executed in Then
            .When(m => new Action(() => Parser.ParseLine($"O [t] {m} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();

    [Fact]
    public Task Unknown_Token_Throws()
        => Given(() => "O [t] %weird -- x")
            .When(l => new Action(() => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();

    [Theory]
    [InlineData("=10x")] // invalid unit
    [InlineData("=foo")] // not matching regex
    [InlineData("=1w")]  // unsupported unit
    [InlineData("=m")]   // missing number
    [InlineData("=10")]  // missing unit
    public Task Bad_Duration_Throws(string dur)
        => Given(() => dur)
            .When(d => new Action(() => Parser.ParseLine($"O [t] {d} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.Throws<FormatException>(a))
            .AssertPassed();
}