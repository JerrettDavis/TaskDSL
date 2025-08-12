namespace TaskDsl.Tests;

public class QuotingAndEscapingTests
{
    [Fact]
    public void Quoted_Assignee_And_Tag_Are_Supported()
    {
        var line = "O [t] ^\"sam j\" -\"support team\" -- Title";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Contains("sam j", t.Assignees);
        Assert.Contains("support team", t.Tags);
    }

    [Fact]
    public void Title_Can_Contain_DoubleDash_When_Escaped()
    {
        var line = "O [t] -- Title with \\-- inside";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal("Title with -- inside", t.Title);
    }
}