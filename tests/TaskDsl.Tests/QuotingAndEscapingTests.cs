using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class QuotingAndEscapingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public Task Quoted_Assignee_And_Tag_Are_Supported()
        => Given(() => "O [t] ^\"sam j\" -\"support team\" -- Title")
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t =>
            {
                Assert.Contains("sam j", t.Assignees);
                Assert.Contains("support team", t.Tags);
            })
            .AssertPassed();

    [Fact]
    public Task Title_Can_Contain_DoubleDash_When_Escaped()
        => Given(() => "O [t] -- Title with \\-- inside")
            .When(l => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t => Assert.Equal("Title with -- inside", t.Title))
            .AssertPassed();
}